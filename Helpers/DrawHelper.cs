using System.Reflection;
using Microsoft.Xna.Framework.Graphics;

namespace Stataria
{
    public static class DrawHelper
    {
        private static readonly FieldInfo beginCalledField = 
            typeof(SpriteBatch).GetField("beginCalled", BindingFlags.Instance | BindingFlags.NonPublic) ?? 
            typeof(SpriteBatch).GetField("_beginCalled", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool IsSpriteBatchActive(SpriteBatch spriteBatch)
        {
            if (spriteBatch == null)
                return false;

            if (beginCalledField == null)
                return true; // Fallback to true if field is not found to avoid breaking functionality in future versions

            return (bool)beginCalledField.GetValue(spriteBatch);
        }
    }
}
