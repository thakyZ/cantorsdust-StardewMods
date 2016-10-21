﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;

namespace TimeSpeed.Components
{
    public interface ITimeFreezerConfig
    {
        int? FreezeTimeAt { get; set; }
        
        bool FreezeTimeIndoors { get; set; }

        bool FreezeTimeInMines { get; set; }

        bool FreezeTimeOutdoors { get; set; }

        Keys FreezeTimeKey { get; set; }
    }
}