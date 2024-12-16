#region header
// WaybackDiffViewer
// Copyright (C)  2024 canadian
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WaybackDiffViewer;

public class DataKeeper
{
    private WaybackMachineApi.WaybackResult[] _waybackResults;
    
    
    private DataKeeper(WaybackMachineApi.WaybackResult[] results)
    {
        this._waybackResults = results;
    }

    public static async Task<DataKeeper> From(string webPage, DateTime oldestAllowed, DateTime newestAllowed)
    {
        List<WaybackMachineApi.WaybackResult> ret = [];
        await foreach (var result in WaybackMachineApi.PerformSearchAsync(webPage))
        {
            ret.Add(result);
        }

        return new DataKeeper(ret.ToArray());
    }

    public WaybackMachineApi.WaybackResult[] Results => _waybackResults;

    public bool IsEmpty() => _waybackResults.Length == 0;
}