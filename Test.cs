using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;

namespace Stataria
{
    public class Test
    {
        public void SomeMethod()
        {
            StatariaLogger.Info("This is an information message");

            StatariaLogger.Warning("This is a warning message");

            StatariaLogger.Error("This is an error message");

            StatariaLogger.Debug("This message only appears in debug mode");

            StatariaLogger.Debug("This message uses global debug mode");
        }
    }
}