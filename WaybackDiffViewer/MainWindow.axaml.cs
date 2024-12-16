using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using DiffPlex.DiffBuilder.Model;

namespace WaybackDiffViewer;

public partial class MainWindow : Window
{
    private DataKeeper? _dataKeeper;

    private Color DeletedColor()
    {
        var color = Color.Parse("#ff8886").ToHsl();
        if (this.ActualThemeVariant == ThemeVariant.Dark)
        {
            color = HslColor.FromHsl(color.H, color.S, 1d - color.L);
        }

        return color.ToRgb();
    }

    private Color InsertedColor()
    {
        var color = Color.Parse("#96d4af").ToHsl();
        if (this.ActualThemeVariant == ThemeVariant.Dark)
        {
            color = HslColor.FromHsl(color.H, color.S, 1d - color.L);
        }

        return color.ToRgb();
    }

    private Color ModifiedColor()
    {
        var color = Color.Parse("#90d5ff").ToHsl();
        if (this.ActualThemeVariant == ThemeVariant.Dark)
        {
            color = HslColor.FromHsl(color.H, color.S, 1d - color.L);
        }

        return color.ToRgb();
    }

    public MainWindow()
    {
        this.SizeToContent = SizeToContent.WidthAndHeight;
        InitializeComponent();
    }

    private async void SubmitButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(UrlInput.Text))
        {
            SubmitButton.IsEnabled = false;
            UrlInput.IsEnabled = false;

            ProgressText.Text = "Loading...";
            _dataKeeper = await DataKeeper.From(UrlInput.Text, DateTime.MinValue, DateTime.MaxValue);
            if (_dataKeeper.IsEmpty())
            {
                ProgressText.Text = "No results found, try again.";
                SubmitButton.IsEnabled = true;
                UrlInput.IsEnabled = true;
                return;
            }

            ProgressText.Text = "Loaded. Processing Diff.";

            LeftDateSelector.ItemsSource = _dataKeeper.Results;
            LeftDateSelector.SelectedIndex = 0;
            RightDateSelector.ItemsSource = _dataKeeper.Results;
            RightDateSelector.SelectedIndex = _dataKeeper.Results.Length - 1;
            
            await LoadDiffAsync(_dataKeeper.Results.First(), _dataKeeper.Results.Last());
        }
        else
        {
            ProgressText.Text = "Invalid URL, try again.";
            return;
        }
    }


    private async Task LoadDiffAsync(WaybackMachineApi.WaybackResult left, WaybackMachineApi.WaybackResult right)
    {
        var diff = DiffPlex.DiffBuilder.SideBySideDiffBuilder.Instance.BuildDiffModel(
            await left.GetMarkdownRepresentationAsync(), await right.GetMarkdownRepresentationAsync(), false);

        SearchView.IsVisible = false;
        DataView.IsVisible = true;

        var deletedColor = DeletedColor().ToString();
        var insertedColor = InsertedColor().ToString();
        var modifiedColor = ModifiedColor().ToString();

        var leftText = new StringBuilder();

        foreach (var line in diff.OldText.Lines)
        {
            // if (line.Type == ChangeType.Modified)
            // {
            //     int deletedCount = 0, insertedCount = 0;
            //     foreach (var piece in line.SubPieces)
            //     {
            //         switch (piece.Type)
            //         {
            //             case ChangeType.Deleted:
            //                 deletedCount += 1;
            //                 break;
            //             case ChangeType.Inserted:
            //                 insertedCount += 1;
            //                 break;
            //         }
            //     }
            //
            //     if (deletedCount > insertedCount*4)
            //     {
            //         line.Type = ChangeType.Deleted;
            //     } else if (insertedCount > deletedCount*4)
            //     {
            //         line.Type = ChangeType.Inserted;
            //     }
            // }
            switch (line.Type)
            {
                case ChangeType.Deleted:
                    leftText.AppendLine(ColorizeString(line.Text, deletedColor));
                    break;
                case ChangeType.Inserted:
                    leftText.AppendLine(ColorizeString(line.Text, insertedColor));
                    break;
                case ChangeType.Modified:
                    foreach (var subpiece in line.SubPieces)
                    {
                        switch (subpiece.Type)
                        {
                            case ChangeType.Deleted:
                                leftText.Append(ColorizeString(subpiece.Text, deletedColor));
                                break;
                            case ChangeType.Inserted:
                                leftText.Append(ColorizeString(subpiece.Text, insertedColor));
                                break;
                            case ChangeType.Modified:
                                leftText.Append(ColorizeString(subpiece.Text, modifiedColor));
                                break;
                            default:
                                leftText.Append($"{subpiece.Text}");
                                break;
                        }
                    }
                    break;
                default:
                    leftText.AppendLine($"{line.Text}");
                    break;
            }
            leftText.AppendLine();
        }

        var rightText = new StringBuilder();
        
        foreach (var line in diff.NewText.Lines)
        {
            // if (line.Type == ChangeType.Modified)
            // {
            //     int deletedCount = 0, insertedCount = 0;
            //     foreach (var piece in line.SubPieces)
            //     {
            //         switch (piece.Type)
            //         {
            //             case ChangeType.Deleted:
            //                 deletedCount += 1;
            //                 break;
            //             case ChangeType.Inserted:
            //                 insertedCount += 1;
            //                 break;
            //         }
            //     }
            //
            //     if (deletedCount > insertedCount*2)
            //     {
            //         line.Type = ChangeType.Deleted;
            //     } else if (insertedCount > deletedCount*2)
            //     {
            //         line.Type = ChangeType.Inserted;
            //     }
            // }
            switch (line.Type)
            {
                case ChangeType.Deleted:
                    rightText.AppendLine(ColorizeString(line.Text, deletedColor));
                    break;
                case ChangeType.Inserted:
                    rightText.AppendLine(ColorizeString(line.Text, insertedColor));
                    break;
                case ChangeType.Modified:
                    foreach (var subpiece in line.SubPieces)
                    {
                        switch (subpiece.Type)
                        {
                            case ChangeType.Deleted:
                                rightText.Append(ColorizeString(subpiece.Text, deletedColor));
                                break;
                            case ChangeType.Inserted:
                                rightText.Append(ColorizeString(subpiece.Text, insertedColor));
                                break;
                            case ChangeType.Modified:
                                rightText.Append(ColorizeString(subpiece.Text, modifiedColor));
                                break;
                            default:
                                rightText.Append($"{subpiece.Text}");
                                break;
                        }
                    }
                    break;
                default:
                    rightText.AppendLine($"{line.Text}");
                    break;
            }
            rightText.AppendLine();
        }

        LeftTitle.Text = $"{left.Timestamp} - {left.OriginalUrl}";
        LeftViewer.Markdown = leftText.ToString();

        RightTitle.Text = $"{right.Timestamp} - {right.OriginalUrl}";
        RightViewer.Markdown = rightText.ToString();
        LoadingText.IsVisible = false;
        NewUrlButton.IsVisible = true;
    }
    
    private async void OnDateChanged(object? sender, EventArgs eventArgs)
    {
        if (LeftDateSelector.SelectedItem != null && RightDateSelector.SelectedItem != null)
        {
            LoadingText.IsVisible = true;
            NewUrlButton.IsVisible = false;
            await LoadDiffAsync((WaybackMachineApi.WaybackResult)LeftDateSelector.SelectedItem, (WaybackMachineApi.WaybackResult)RightDateSelector.SelectedItem);    
        }
    }
    
    private Vector _lastScrollLeft = Vector.One;
    private void LeftViewerScrolled(object? sender, EventArgs eventArgs)
    {
        if (LeftViewer.ScrollValue == _lastScrollLeft) return;
        RightViewer.ScrollValue = LeftViewer.ScrollValue;
        _lastScrollLeft = LeftViewer.ScrollValue;
    }
    
    private Vector _lastScrollRight = Vector.One;
    private void RightViewerScrolled(object? sender, EventArgs eventArgs)
    {
        if (RightViewer.ScrollValue == _lastScrollRight) return;
        LeftViewer.ScrollValue = RightViewer.ScrollValue;
        _lastScrollRight = RightViewer.ScrollValue;
    }

    private static Regex _headerRegex = new Regex(@"#+ .*");
    private string ColorizeString(string input, string color)
    {
        var ret = new StringBuilder();
        if (_headerRegex.IsMatch(input)) {
            var split = input.Split(' ', 2);
            ret.Append(split[0]);
            ret.Append("%{background:");
            ret.Append(color);
            ret.Append('}');
            ret.Append(split[1]);
            ret.Append('%');
        }
        else
        {
            ret.Append("%{background:");
            ret.Append(color);
            ret.Append('}');
            ret.Append(input);
            ret.Append('%');
        }

        return ret.ToString();
    }

    private void NewUrlButton_OnClick(object? sender, RoutedEventArgs e)
    {
        DataView.IsVisible = false;
        SearchView.IsVisible = true;
        UrlInput.Text = "";
        UrlInput.IsEnabled = true;
        ProgressText.Text = "Waiting for URL";
        SubmitButton.IsEnabled = true;
    }
}