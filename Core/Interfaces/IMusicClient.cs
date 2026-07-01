using System;
using System.Collections.Generic;
using System.Text;
using Core.Models;

namespace Core.Interfaces
{
    public interface IMusicClient
    {
        Task<IReadOnlyList<Song>> SearchAsync(string query);
    }
}
