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
using Terraria.Localization;

namespace Stataria
{
    public class Test
    {
        public void SomeMethod()
        {
            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Logging.Test.InfoMessage"));

            StatariaLogger.Warning(Language.GetTextValue("Mods.Stataria.Logging.Test.WarningMessage"));

            StatariaLogger.Error(Language.GetTextValue("Mods.Stataria.Logging.Test.ErrorMessage"));

            StatariaLogger.Debug(Language.GetTextValue("Mods.Stataria.Logging.Test.DebugMessage"));

            StatariaLogger.Debug(Language.GetTextValue("Mods.Stataria.Logging.Test.GlobalDebugMessage"));
        }
    }
}