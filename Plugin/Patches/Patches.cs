using BepInEx;
using GameChat.UI;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LordAshes
{
	public partial class TaleSpireEmbeddedCharacterSheetPlugin : BaseUnityPlugin
	{
		/// <summary>
		/// Path AddDiceResultMessage To Add Proper Roller Name 
		/// </summary>
		[HarmonyPatch(typeof(UIChatMessageManager), "AddDiceResultMessage")]
		public static class PatchAddDiceResultMessage
		{
			public static bool Prefix(DiceManager.DiceRollResultData diceResult, ClientGuid sender)
			{
				return false;
			}

			public static void Postfix(ref UIChatMessageManager __instance, DiceManager.DiceRollResultData diceResult, ClientGuid sender, List<UIChatMessageManager.MessageReference> ____messagelist)
			{
				string playerName = "";
				PlayerGuid key;
				if (BoardSessionManager.ClientsPlayerGuids.TryGetValue(sender, out key))
				{
					PlayerInfo playerInfo;
					if (CampaignSessionManager.PlayersInfo.TryGetValue(key, out playerInfo))
					{
						playerName = ((sender == LocalClient.Id) ? "<color=green>YOU</color>" : (playerInfo.Name ?? "<unknown>"));
					}
					else
					{
						playerName = "<unknown>";
					}
				}
				if (diceResult.GroupResults[0].Name.StartsWith("[") && diceResult.GroupResults[0].Name.Contains("]"))
				{
					playerName = diceResult.GroupResults[0].Name.Substring(0, diceResult.GroupResults[0].Name.IndexOf("]")) + " (" + playerName + ")";
					playerName = playerName.Replace("[", "").Replace("]", "");
					diceResult.GroupResults[0] = new DiceManager.DiceGroupResultData
					(
						diceResult.GroupResults[0].Name.Substring(diceResult.GroupResults[0].Name.IndexOf("]") + 1),
						diceResult.GroupResults[0].Dice
					);
				}
				____messagelist.Add(new UIChatMessageManager.DiceResultsReference
				{
					PlayerName = playerName,
					RollResult = diceResult
				});
				if (__instance.gameObject.activeInHierarchy)
				{
					__instance.QueueUpdateStack();
				}
			}
		}

		/// <summary>
		/// Patch RPC_DiceResult To Remove Temp Roller Name From Result
		/// </summary>
		[HarmonyPatch(typeof(DiceManager), "RPC_DiceResult")]
		public static class PatchDisplayResult
		{
			public static bool Prefix(bool isGmOnly, byte[] diceListData, PhotonMessageInfo msgInfo)
			{
				return false;
			}

			public static void Postfix(ref DiceManager __instance, bool isGmOnly, byte[] diceListData, PhotonMessageInfo msgInfo)
			{
				if (BoardSessionManager.InBoard)
				{
					DiceManager.DiceRollResultData diceRollResultData = BinaryIO.FromByteArray<DiceManager.DiceRollResultData>(diceListData, (BinaryReader br) => br.ReadDiceRollResultData());
					ClientGuid clientGuid;
					PlayerGuid playerGuid;
					PhotonConnectionManager.PhotonUserIdStringToIds(msgInfo.sender.UserId).Deconstruct(out clientGuid, out playerGuid);
					ClientGuid sender = clientGuid;
					if (!isGmOnly || LocalClient.IsInGmMode)
					{
						SingletonBehaviour<GUIManager>.Instance.Chat.AddDiceResultMessage(diceRollResultData, sender);
						DiceManager.DiceGroupResultData[] dgrd = diceRollResultData.GroupResults;
						if(dgrd[0].Name.StartsWith("["))
                        {
							dgrd[0] = new DiceManager.DiceGroupResultData(
								dgrd[0].Name.Substring(dgrd[0].Name.IndexOf("]") + 1),
								dgrd[0].Dice
							);
						}
						DiceManager.DiceRollResultData adjRoll = new DiceManager.DiceRollResultData(diceRollResultData.RollId, dgrd);
						GUIManager.DiceRollResult.DisplayResult(adjRoll, sender);
					}
				}
			}
		}
	}
}