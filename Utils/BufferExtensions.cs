using System.Text;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;

namespace BanchoNET.Utils;

public static class BufferExtensions
{
	public static string ReadOsuString(this BinaryReader br)
	{
		var stringExists = br.ReadByte();
		if (stringExists == 0) return string.Empty;
		if (stringExists != 11)
		{
			Console.WriteLine("[BufferExtensions] Invalid byte value while trying to read osu string");
			return string.Empty;
		}
		
		return Encoding.UTF8.GetString(br.ReadBytes(br.ReadULEB128()));
	}

	public static Message ReadOsuMessage(this BinaryReader br)
	{
		return new Message
		{
			Sender = br.ReadOsuString(),
			Content = br.ReadOsuString(),
			Destination = br.ReadOsuString(),
			SenderId = br.ReadInt32()
		};
	}
	
	/// <summary>
	/// Reads osu! list of 32 bit integers with length encoded as 16 bit integer
	/// </summary>
	/// <returns>List of integer values</returns>
	public static List<int> ReadOsuList32(this BinaryReader br)
	{
		var returnList = new List<int>();
		var length = br.ReadUInt16();

		while (length > 0)
		{
			returnList.Add(br.ReadInt32());
			length--;
		}

		return returnList;
	}

	public static void WriteOsuList32(this BinaryWriter bw, List<int> list)
	{
		bw.Write((ushort)list.Count);
		
		foreach (var id in list)
			bw.Write(id);
	}
	
	public static void WriteUserStats(this BinaryWriter bw, Player player)
	{
		var modeStats = player.Stats[player.Status.Mode];
		var modeRankedScore = modeStats.RankedScore;
		var modePP = modeStats.PP;

		if (modeStats.PP > short.MaxValue)
		{
			modeRankedScore = modePP;
			modePP = 0;
		}
		
		bw.Write(player.Id);
		bw.Write((byte)player.Status.Activity);
		bw.WriteOsuString(player.Status.ActivityDescription);
		bw.WriteOsuString(player.Status.BeatmapMD5);
		bw.Write((int)player.Status.CurrentMods);
		bw.Write((byte)player.Status.Mode.AsVanilla());
		bw.Write(player.Status.BeatmapId);
		bw.Write(modeRankedScore);
		bw.Write(modeStats.Accuracy / 100);
		bw.Write(modeStats.PlayCount);
		bw.Write(modeStats.TotalScore);
		bw.Write(modeStats.Rank);
		bw.Write(modePP);
	}
	
	//TODO make it modifiable by user(?)
	private static readonly List<(Activity Activity, string Description)> BotStatuses =
	[
		(Activity.Afk, "looking for source.."),
		(Activity.Editing, "the source code.."),
		(Activity.Editing, "server's website.."),
		(Activity.Modding, "your requests.."),
		(Activity.Watching, "over all of you.."),
		(Activity.Watching, "over the server.."),
		(Activity.Testing, "my will to live.."),
		(Activity.Testing, "your patience.."),
		(Activity.Submitting, "scores to database.."),
		(Activity.Submitting, "a pull request.."),
		(Activity.OsuDirect, "updating maps..")
	];

	public static void WriteBotStats(this BinaryWriter bw, Player player)
	{
		var status = BotStatuses[Random.Shared.Next(0, BotStatuses.Count)];
		
		bw.Write(player.Id);
		bw.Write((byte)status.Activity);
		bw.WriteOsuString(status.Description);
		bw.WriteOsuString("");
		bw.Write((int)0);
		bw.Write((byte)0);
		bw.Write((int)0);
		bw.Write((long)0);
		bw.Write(0.0f);
		bw.Write((int)0);
		bw.Write((long)0);
		bw.Write((int)0);
		bw.Write((short)0);
	}
	
	public static void WriteUserPresence(this BinaryWriter bw, Player player)
	{
		bw.Write(player.Id);
		bw.WriteOsuString(player.Username);
		bw.Write(player.TimeZone);
		bw.Write((byte)player.Geoloc.Country.Numeric);
		bw.Write((byte)((int)player.ToBanchoPrivileges() | ((int)player.Status.Mode.AsVanilla() << 5)));
		bw.Write(player.Geoloc.Longitude);
		bw.Write(player.Geoloc.Latitude);
		bw.Write(player.Stats[player.Status.Mode].Rank);
	}

	public static void WriteBotPresence(this BinaryWriter bw, Player player)
	{
		bw.Write(player.Id);
		bw.WriteOsuString(player.Username);
		bw.Write((byte)1);
		bw.Write((byte)245);
		bw.Write((byte)31);
		bw.Write(6669.420f);
		bw.Write(727.27f);
		bw.Write((int)0);
	}

	public static void WriteOsuString(this BinaryWriter bw, string? data)
	{
		if (data == null)
			bw.Write((byte)0);
		else
		{
			bw.Write((byte)11);

			var stringBytes = Encoding.UTF8.GetBytes(data);
			bw.WriteULEB128(stringBytes.Length);
			bw.Write(stringBytes);
		}
	}

	public static void WriteOsuMessage(this BinaryWriter bw, Message message)
	{
		bw.WriteOsuString(message.Sender);
		bw.WriteOsuString(message.Content);
		bw.WriteOsuString(message.Destination);
		bw.Write(message.SenderId);
	}
	
	public static void WriteOsuChannel(this BinaryWriter bw, Channel channel)
	{
		bw.WriteOsuString(channel.Name);
		bw.WriteOsuString(channel.Description);
		bw.Write((ushort)channel.Players.Count);
	}

	public static void WriteOsuMatch(this BinaryWriter bw, MultiplayerLobby lobby)
	{
		//TODO
	}
}