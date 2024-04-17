using System.Text;
using BanchoNET.Objects;
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
	
	public static MultiplayerLobby ReadOsuMatch(this BinaryReader br)
	{
		var match = new MultiplayerLobby
		{
			Id = br.ReadUInt16(),
			InProgress = br.ReadByte() == 1,
			Powerplay = br.ReadByte(),
			Mods = (Mods)br.ReadInt32(),
			Name = br.ReadOsuString(),
			Password = br.ReadOsuString(),
			BeatmapName = br.ReadOsuString(),
			BeatmapId = br.ReadInt32(),
			BeatmapMD5 = br.ReadOsuString(),
		};

		var slots = match.Slots;

		for (int i = 0; i < slots.Length; i++)
			slots[i].Status = (SlotStatus)br.ReadByte();

		for (int i = 0; i < slots.Length; i++)
			slots[i].Team = (LobbyTeams)br.ReadByte();

		for (int i = 0; i < slots.Length; i++)
			if ((slots[i].Status & SlotStatus.PlayerInSlot) != 0)
				br.ReadInt32();

		match.HostId = br.ReadInt32();
		match.Mode = (GameMode)br.ReadByte();
		match.WinCondition = (WinCondition)br.ReadByte();
		match.Type = (LobbyType)br.ReadByte();
		match.Freemods = br.ReadByte() == 1;
		
		if (match.Freemods)
			for (int i = 0; i < slots.Length; i++)
				slots[i].Mods = (Mods)br.ReadInt32();

		match.Slots = slots;
		match.Seed = br.ReadInt32();
		
		return match;
	}

	/// <summary>
	/// Reads raw data from the osu packet that had its length already read
	/// </summary>
	public static byte[] ReadRawData(this BinaryReader br)
	{
		br.BaseStream.Position -= 4;
		
		var dataLength = br.ReadInt32();
		return br.ReadBytes(dataLength);
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
		bw.Write(player.TimeZone + 24);
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

	public static void WriteOsuMatch(this BinaryWriter bw, LobbyData lobbyData)
	{
		var match = lobbyData.Lobby;
		
		bw.Write(match.Id);
		bw.Write((byte)(match.InProgress ? 1 : 0));
		bw.Write((byte)0);
		bw.Write((uint)match.Mods);
		bw.WriteOsuString(match.Name);

		if (!string.IsNullOrEmpty(match.Password))
		{
			if (lobbyData.SendPassword)
				bw.WriteOsuString(match.Password);
			else
			{
				bw.Write((byte)11);
				bw.Write((byte)0);
			}
		}
		else
			bw.Write((byte)0);
		
		bw.WriteOsuString(match.BeatmapName);
		bw.Write(match.BeatmapId);
		bw.WriteOsuString(match.BeatmapMD5);

		var slots = match.Slots;
		for (int i = 0; i < slots.Length; i++)
			bw.Write((byte)slots[i].Status);
		
		for (int i = 0; i < slots.Length; i++)
			bw.Write((byte)slots[i].Team);
		
		for (int i = 0; i < slots.Length; i++)
		{
			if ((slots[i].Status & SlotStatus.PlayerInSlot) == 0) continue;
			
			var player = slots[i].Player;
			
			if (player != null)
				bw.Write((uint)player.Id);
		}
		
		bw.Write((uint)match.HostId);
		bw.Write((byte)match.Mode);
		bw.Write((byte)match.WinCondition);
		bw.Write((byte)match.Type);
		bw.Write((byte)(match.Freemods ? 1 : 0));
		
		if (match.Freemods)
			for (int i = 0; i < slots.Length; i++)
				bw.Write((uint)slots[i].Mods);
		
		bw.Write((uint)match.Seed);
	}
}