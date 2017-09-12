﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fuxion
{
    public class FileSystemStoredTimeProvider : StoredTimeProvider
    {
        public string Path { get; set; } = @"Software\Fuxion\RegistryStorageTimeProvider";
        public string FileName { get; set; } = "Value";
        public bool EncryptContent { get; set; }

        public override DateTime GetUtcTime()
        {
            throw new NotImplementedException();
        }
        public override void SaveUtcTime(DateTime time)
        {
            throw new NotImplementedException();
        }
    }
}
