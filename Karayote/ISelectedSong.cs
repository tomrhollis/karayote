﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Karayote
{
    internal interface ISelectedSong
    {
        string Id { get; } // stringified uint for karafun, video id for youtube
        string Title { get; } // artist and song name for karafun, page title for youtube    
    }
}
