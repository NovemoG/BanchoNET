using System.Text;
using BanchoNET.Objects;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Replay;

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
	public static List<int> ReadOsuListInt32(this BinaryReader br)
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
			InProgress = br.ReadBoolean(),
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
		match.Freemods = br.ReadBoolean();
		
		if (match.Freemods)
			for (int i = 0; i < slots.Length; i++)
				slots[i].Mods = (Mods)br.ReadInt32();

		match.Slots = slots;
		match.Seed = br.ReadInt32();
		
		return match;
	}

	public static SpectateFrames ReadSpectateFrames(this BinaryReader br)
	{
		return new SpectateFrames
		{
			Extra = br.ReadInt32(),
			ReplayFrames = br.ReadReplayFrames(br.ReadUInt16()),
			Action = (ReplayAction)br.ReadByte(),
			ScoreFrame = br.ReadScoreFrame(),
			Sequence = br.ReadUInt16()
		};
	}
	
	private static List<ReplayFrame> ReadReplayFrames(this BinaryReader br, int frameCount)
	{
		var returnFrames = new List<ReplayFrame>();

		for (int i = 0; i < frameCount; i++)
		{
			returnFrames.Add(new ReplayFrame
			{
				ButtonState = br.ReadByte(),
				TaikoByte = br.ReadByte(),
				MouseX = br.ReadSingle(),
				MouseY = br.ReadSingle(),
				Time = br.ReadInt32()
			});
		}

		return returnFrames;
	}

	private static ScoreFrame ReadScoreFrame(this BinaryReader br)
	{
		var scoreFrame = new ScoreFrame
		{
			Time = br.ReadInt32(),
			Id = br.ReadByte(),
			Count300 = br.ReadUInt16(),
			Count100 = br.ReadUInt16(),
			Count50 = br.ReadUInt16(),
			Gekis = br.ReadUInt16(),
			Katus = br.ReadUInt16(),
			Misses = br.ReadUInt16(),
			TotalScore = br.ReadInt32(),
			MaxCombo = br.ReadUInt16(),
			CurrentCombo = br.ReadUInt16(),
			Perfect = br.ReadBoolean(),
			CurrentHp = br.ReadByte(),
			TagByte = br.ReadByte(),
			ScoreV2 = br.ReadBoolean()
		};

		if (scoreFrame.ScoreV2)
		{
			scoreFrame.ComboPortion = br.ReadDouble();
			scoreFrame.BonusPortion = br.ReadDouble();
		}

		return scoreFrame;
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
		bw.Write((short)modePP);
	}

	public static void WriteBotStats(this BinaryWriter bw, Player player)
	{
		bw.Write(player.Id);
		bw.Write((byte)player.Status.Activity);
		bw.WriteOsuString(player.Status.ActivityDescription);
		bw.WriteOsuString(null);
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
		bw.Write((byte)(player.TimeZone + 24));
		bw.Write((byte)player.Geoloc.Country.Numeric);
		bw.Write((byte)((int)player.ToBanchoPrivileges() | ((int)player.Status.Mode.AsVanilla() << 5)));
		bw.Write(player.Geoloc.Longitude);
		bw.Write(player.Geoloc.Latitude);
		bw.Write(player.Stats[player.Status.Mode].Rank);
	}

	public static void WriteBotPresence(this BinaryWriter bw, Player player)
	{
		var timezone = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
		
		bw.Write(player.Id);
		bw.WriteOsuString(player.Username);
		bw.Write((byte)(timezone.Hours + 24));
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
		bw.Write(match.InProgress);
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
		bw.Write(match.Freemods);
		
		if (match.Freemods)
			for (int i = 0; i < slots.Length; i++)
				bw.Write((uint)slots[i].Mods);
		
		bw.Write((uint)match.Seed);
	}
	
	public static void WriteSpectateFrames(this BinaryWriter bw, SpectateFrames frames)
	{
		bw.Write(frames.Extra);
		bw.Write((ushort)frames.ReplayFrames.Count);
		
		foreach (var frame in frames.ReplayFrames)
			bw.WriteReplayFrame(frame);
		
		bw.Write((byte)frames.Action);
		bw.WriteScoreFrame(frames.ScoreFrame);
		bw.Write(frames.Sequence);
	}
	
	private static void WriteScoreFrame(this BinaryWriter bw, ScoreFrame frame)
	{
		bw.Write(frame.Time);
		bw.Write(frame.Id);
		bw.Write(frame.Count300);
		bw.Write(frame.Count100);
		bw.Write(frame.Count50);
		bw.Write(frame.Gekis);
		bw.Write(frame.Katus);
		bw.Write(frame.Misses);
		bw.Write(frame.TotalScore);
		bw.Write(frame.MaxCombo);
		bw.Write(frame.CurrentCombo);
		bw.Write(frame.Perfect);
		bw.Write(frame.CurrentHp);
		bw.Write(frame.TagByte);
		bw.Write(frame.ScoreV2);
		
		if (frame.ScoreV2)
		{
			bw.Write(frame.ComboPortion);
			bw.Write(frame.BonusPortion);
		}
	}
	
	private static void WriteReplayFrame(this BinaryWriter bw, ReplayFrame frame)
	{
		bw.Write(frame.ButtonState);
		bw.Write(frame.TaikoByte);
		bw.Write(frame.MouseX);
		bw.Write(frame.MouseY);
		bw.Write(frame.Time);
	}
}