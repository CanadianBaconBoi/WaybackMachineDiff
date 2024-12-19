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

            ProgressText.Text = DoDeduplication.IsChecked == true ? "Loading and Deduplicating, please be patient." : "Loading...";
            _dataKeeper = await DataKeeper.From(UrlInput.Text, DoDeduplication.IsChecked ?? false, DoCacheResults.IsChecked ?? false);
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
        }
    }


    private async Task LoadDiffAsync(WaybackMachineApi.WaybackResult left, WaybackMachineApi.WaybackResult right)
    {
        var diff = DiffPlex.DiffBuilder.SideBySideDiffBuilder.Instance.BuildDiffModel(
            await left.GetMarkdownRepresentationAsync(), await right.GetMarkdownRepresentationAsync(), true);

        SearchView.IsVisible = false;
        DataView.IsVisible = true;
        
        LeftTitle.Text = $"{left.Timestamp} - {left.OriginalUrl}";
        RightTitle.Text = $"{right.Timestamp} - {right.OriginalUrl}";

        (LeftViewer.Markdown, RightViewer.Markdown) = GenerateMarkdownDiff(diff);

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

    [GeneratedRegex("^#+ .*")]
    private static partial Regex HeaderRegex();
    [GeneratedRegex("^#+$")]
    private static partial Regex JustHeaderRegex();

    private string ColorizeString(string? input, string color)
    {
        if (input == null || JustHeaderRegex().IsMatch(input)) return "";
        var ret = new StringBuilder();
        if (HeaderRegex().IsMatch(input)) {
            var split = input.Split(' ', 2);
            ret.Append(split[0]);
            if (split.Length <= 1) return ret.ToString();
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

    private (string leftText, string rightText) GenerateMarkdownDiff(SideBySideDiffModel diff)
    {
        var deletedColor = DeletedColor().ToString();
        var insertedColor = InsertedColor().ToString();
        var modifiedColor = ModifiedColor().ToString();

        var leftText = GenerateMarkdownDiffPane(diff.OldText, deletedColor, insertedColor, modifiedColor);
        var rightText = GenerateMarkdownDiffPane(diff.NewText, deletedColor, insertedColor, modifiedColor);

        return (leftText, rightText);
    }

    private string GenerateMarkdownDiffPane(DiffPaneModel diffPane, string deletedColor, string insertedColor, string modifiedColor)
    {
        var ret = new StringBuilder();

        foreach (var line in diffPane.Lines)
        {
            switch (line.Type)
            {
                case ChangeType.Deleted:
                    ret.AppendLine(ColorizeString(line.Text, deletedColor));
                    break;
                case ChangeType.Inserted:
                    ret.AppendLine(ColorizeString(line.Text, insertedColor));
                    break;
                case ChangeType.Modified:
                    foreach (var subPiece in line.SubPieces)
                    {
                        switch (subPiece.Type)
                        {
                            case ChangeType.Deleted:
                                ret.Append(ColorizeString(subPiece.Text, deletedColor));
                                break;
                            case ChangeType.Inserted:
                                ret.Append(ColorizeString(subPiece.Text, insertedColor));
                                break;
                            default:
                                ret.Append(ColorizeString(subPiece.Text, modifiedColor));
                                break;
                        }
                    }
                    break;
                default:
                    ret.AppendLine($"{line.Text}");
                    break;
            }
            ret.AppendLine();
        }

        return ret.ToString();
    }
}