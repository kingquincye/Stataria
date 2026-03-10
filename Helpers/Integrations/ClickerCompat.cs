using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Stataria
{
	internal class ClickerCompat : ModSystem
	{

		internal static readonly Version apiVersion = new Version(1, 4);

		internal static string versionString;

		private static Mod clickerClass;

		internal static Mod ClickerClass
		{
			get
			{
				if (clickerClass == null && ModLoader.TryGetMod("ClickerClass", out var mod))
				{
					clickerClass = mod;
				}
				return clickerClass;
			}
		}

		public override void Load()
		{
			versionString = apiVersion.ToString();
		}

		public override void Unload()
		{
			clickerClass = null;
			versionString = null;
		}

		#region General Calls
		internal static void SetClickerWeaponDefaults(Item item)
		{
			ClickerClass?.Call("SetClickerWeaponDefaults", versionString, item);
		}

		internal static void SetSFXButtonDefaults(Item item)
		{
			ClickerClass?.Call("SetSFXButtonDefaults", versionString, item);
		}

		internal static void SetClickerProjectileDefaults(Projectile proj)
		{
			ClickerClass?.Call("SetClickerProjectileDefaults", versionString, proj);
		}

		internal static void RegisterClickerProjectile(ModProjectile modProj)
		{
			ClickerClass?.Call("RegisterClickerProjectile", versionString, modProj);
		}

		internal static void RegisterClickerWeaponProjectile(ModProjectile modProj)
		{
			ClickerClass?.Call("RegisterClickerWeaponProjectile", versionString, modProj);
		}

		internal static void RegisterClickerItem(ModItem modItem)
		{
			ClickerClass?.Call("RegisterClickerItem", versionString, modItem);
		}

		internal static void RegisterClickerWeapon(ModItem modItem, string borderTexture = null)
		{
			ClickerClass?.Call("RegisterClickerWeapon", versionString, modItem, borderTexture);
		}

		internal static void RegisterSFXButton(ModItem modItem, Action<int> playSoundAction)
		{
			ClickerClass?.Call("RegisterSFXButton", versionString, modItem, playSoundAction);
		}

		internal static string RegisterClickEffect(Mod mod, string internalName, int amount, Func<Color> colorFunc, Action<Player, EntitySource_ItemUse_WithAmmo, Vector2, int, int, float> action, bool preHardMode = false, object[] nameArgs = null, object[] descriptionArgs = null)
		{
			return ClickerClass?.Call("RegisterClickEffect", versionString, mod, internalName, amount, colorFunc, action, preHardMode, nameArgs, descriptionArgs) as string;
		}

		internal static string RegisterClickEffect(Mod mod, string internalName, int amount, Color color, Action<Player, EntitySource_ItemUse_WithAmmo, Vector2, int, int, float> action, bool preHardMode = false, object[] nameArgs = null, object[] descriptionArgs = null)
		{
			return RegisterClickEffect(mod, internalName, amount, () => color, action, preHardMode, nameArgs, descriptionArgs);
		}

		internal static string GetPathToBorderTexture(int type)
		{
			return ClickerClass?.Call("GetPathToBorderTexture", versionString, type) as string;
		}

		internal static List<string> GetAllEffectNames()
		{
			return ClickerClass?.Call("GetAllEffectNames", versionString) as List<string>;
		}

		internal static Dictionary<string, object> GetClickEffectAsDict(string effect)
		{
			return ClickerClass?.Call("GetClickEffectAsDict", versionString, effect) as Dictionary<string, object>;
		}

		internal static bool IsClickEffect(string effect)
		{
			return ClickerClass?.Call("IsClickEffect", versionString, effect) as bool? ?? false;
		}

		internal static bool IsClickerProj(int type)
		{
			return ClickerClass?.Call("IsClickerProj", versionString, type) as bool? ?? false;
		}

		internal static bool IsClickerProj(Projectile proj)
		{
			return ClickerClass?.Call("IsClickerProj", versionString, proj) as bool? ?? false;
		}

		internal static bool IsClickerWeaponProj(int type)
		{
			return ClickerClass?.Call("IsClickerWeaponProj", versionString, type) as bool? ?? false;
		}

		internal static bool IsClickerWeaponProj(Projectile proj)
		{
			return ClickerClass?.Call("IsClickerWeaponProj", versionString, proj) as bool? ?? false;
		}

		internal static bool IsClickerItem(int type)
		{
			return ClickerClass?.Call("IsClickerItem", versionString, type) as bool? ?? false;
		}

		internal static bool IsClickerItem(Item item)
		{
			return ClickerClass?.Call("IsClickerItem", versionString, item) as bool? ?? false;
		}

		internal static bool IsSFXButton(Item item)
		{
			return ClickerClass?.Call("IsSFXButton", versionString, item) as bool? ?? false;
		}

		internal static bool IsSFXButton(int type)
		{
			return ClickerClass?.Call("IsSFXButton", versionString, type) as bool? ?? false;
		}

		internal static Action<int> GetSFXButton(Item item)
		{
			return ClickerClass?.Call("GetSFXButton", versionString, item) as Action<int> ?? null;
		}

		internal static Action<int> GetSFXButton(int type)
		{
			return ClickerClass?.Call("GetSFXButton", versionString, type) as Action<int> ?? null;
		}

		internal static bool IsClickerWeapon(int type)
		{
			return ClickerClass?.Call("IsClickerWeapon", versionString, type) as bool? ?? false;
		}

		internal static bool IsClickerWeapon(Item item)
		{
			return ClickerClass?.Call("IsClickerWeapon", versionString, item) as bool? ?? false;
		}
		#endregion

		#region Item Calls
		internal static void SetColor(Item item, Color color)
		{
			ClickerClass?.Call("SetColor", versionString, item, color);
		}

		internal static void SetRadius(Item item, float radius)
		{
			ClickerClass?.Call("SetRadius", versionString, item, radius);
		}

		internal static void AddEffect(Item item, string effect)
		{
			ClickerClass?.Call("AddEffect", versionString, item, effect);
		}

		internal static void AddEffect(Item item, IEnumerable<string> effects)
		{
			ClickerClass?.Call("AddEffect", versionString, item, effects);
		}

		internal static void SetDust(Item item, int type)
		{
			ClickerClass?.Call("SetDust", versionString, item, type);
		}

		internal static void SetAccessoryType(Item item, string accessoryType)
		{
			ClickerClass?.Call("SetAccessoryType", versionString, item, accessoryType);
		}

		internal static void SetDisplayTotalClicks(Item item)
		{
			ClickerClass?.Call("SetDisplayTotalClicks", versionString, item);
		}

		internal static void SetDisplayMoneyGenerated(Item item)
		{
			ClickerClass?.Call("SetDisplayMoneyGenerated", versionString, item);
		}
		#endregion

		#region Player Calls
		internal static float GetClickerRadius(Player player)
		{
			return ClickerClass?.Call("GetPlayerStat", versionString, player, "clickerRadius") as float? ?? 1f;
		}

		internal static int GetClickAmount(Player player)
		{
			return ClickerClass?.Call("GetPlayerStat", versionString, player, "clickAmount") as int? ?? 0;
		}

		internal static float GetClickerPerSecond(Player player)
		{
			return ClickerClass?.Call("GetPlayerStat", versionString, player, "clickerPerSecond") as float? ?? 0;
		}

		internal static int GetClickerAmountTotal(Player player, Item item, string effect)
		{
			return ClickerClass?.Call("GetPlayerStat", versionString, player, "clickerAmountTotal", item, effect) as int? ?? 1;
		}

		internal static bool GetArmorSet(Player player, string set)
		{
			return ClickerClass?.Call("GetArmorSet", versionString, player, set) as bool? ?? false;
		}

		internal static bool GetAccessory(Player player, string accessory)
		{
			return ClickerClass?.Call("GetAccessory", versionString, player, accessory) as bool? ?? false;
		}

		internal static void SetAccessory(Player player, string accessory)
		{
			ClickerClass?.Call("SetAccessory", versionString, player, accessory);
		}

		internal static Item GetAccessoryItem(Player player, string accessory)
		{
			return ClickerClass?.Call("GetAccessoryItem", versionString, player, accessory) as Item ?? null;
		}

		internal static void SetAccessoryItem(Player player, string accessory, Item item)
		{
			ClickerClass?.Call("SetAccessoryItem", versionString, player, accessory, item);
		}

		internal static void SetClickerCritAdd(Player player, int add)
		{
			ClickerClass?.Call("SetPlayerStat", versionString, player, "clickerCritAdd", add);
		}

		internal static void SetDamageFlatAdd(Player player, int add)
		{
			ClickerClass?.Call("SetPlayerStat", versionString, player, "clickerDamageFlatAdd", add);
		}

		internal static void SetDamageAdd(Player player, float add)
		{
			ClickerClass?.Call("SetPlayerStat", versionString, player, "clickerDamageAdd", add);
		}

		internal static void SetClickerBonusAdd(Player player, int add)
		{
			ClickerClass?.Call("SetPlayerStat", versionString, player, "clickerBonusAdd", add);
		}

		internal static void SetClickerBonusPercentAdd(Player player, float add)
		{
			ClickerClass?.Call("SetPlayerStat", versionString, player, "clickerBonusPercentAdd", add);
		}

		internal static void SetClickerRadiusAdd(Player player, float add)
		{
			ClickerClass?.Call("SetPlayerStat", versionString, player, "clickerRadiusAdd", add);
		}

		internal static void EnableClickEffect(Player player, string effect)
		{
			ClickerClass?.Call("EnableClickEffect", versionString, player, effect);
		}

		internal static void EnableClickEffect(Player player, IEnumerable<string> effects)
		{
			ClickerClass?.Call("EnableClickEffect", versionString, player, effects);
		}

		internal static bool HasClickEffect(Player player, string effect)
		{
			return ClickerClass?.Call("HasClickEffect", versionString, player, effect) as bool? ?? false;
		}

		internal static IReadOnlyDictionary<int, int> GetAllSFXButtonStacks(Player player)
		{
			return ClickerClass?.Call("GetAllSFXButtonStacks", versionString, player) as IReadOnlyDictionary<int, int> ?? null;
		}

		internal static bool AddSFXButtonStack(Player player, Item item)
		{
			return ClickerClass?.Call("AddSFXButtonStack", versionString, player, item) as bool? ?? false;
		}

		internal static bool AddSFXButtonStack(Player player, int type, int stack)
		{
			return ClickerClass?.Call("AddSFXButtonStack", versionString, player, type, stack) as bool? ?? false;
		}

		internal static void SetAutoReuseEffect(Player player, float speedFactor, bool controlledByKeyBind = false, bool preventsClickEffects = false)
		{
			ClickerClass?.Call("SetAutoReuseEffect", versionString, player, speedFactor, controlledByKeyBind, preventsClickEffects);
		}
		#endregion
	}
}