using System;
using System.Collections.Generic;
using System.Text;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace InnerTube.Services
{
    public class InnerTubeClient : MusicClient
    {
        public Task<IReadOnlyList<Song>> SearchAsync(string query)
        {
            throw new NotImplementedException();
        }
    }

}
