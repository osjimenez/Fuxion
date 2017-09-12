﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fuxion.Identity
{
    public interface IPasswordProvider
    {
        bool Verify(string password, byte[] hash, byte[] salt);
        void Generate(string password, out byte[] salt, out byte[] hash);
    }
}
