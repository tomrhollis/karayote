﻿using Microsoft.AspNetCore.Identity;

namespace Karayote.Models
{
    public class User : IdentityUser
    {
        public ICollection<SongRequest> SongRequests { get; set; }
    }
}