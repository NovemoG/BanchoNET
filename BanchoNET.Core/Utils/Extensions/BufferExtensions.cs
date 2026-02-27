using System.Text;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Replay;
using BanchoNET.Core.Models.Users;
using Novelog;

namespace BanchoNET.Core.Utils.Extensions;

public static class BufferExtensions
{
	extension(
		BinaryReader br
	) {
		public string ReadOsuString()
		{
			var stringExists = br.ReadByte();
			if (stringExists == 0) return string.Empty;
			if (stringExists != 11)
			{
				Logger.Shared.LogWarning("Invalid byte value while trying to read osu string", caller: nameof(BufferExtensions));
				return string.Empty;
			}
		
			return Encoding.UTF8.GetString(br.ReadBytes(br.ReadULEB128()));
		}

		public Message ReadOsuMessage()
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
		public List<int> ReadOsuListInt32()
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

		public MultiplayerMatch ReadOsuMatch()
		{
			var match = new MultiplayerMatch
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

		public SpectateFrames ReadSpectateFrames()
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

		private List<ReplayFrame> ReadReplayFrames(
			int frameCount
		) {
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

		private ScoreFrame ReadScoreFrame()
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
		public byte[] ReadRawData()
		{
			br.BaseStream.Position -= 4;
		
			var dataLength = br.ReadInt32();
			return br.ReadBytes(dataLength);
		}
	}

	extension(
		BinaryWriter bw
	) {
		public void WriteOsuList32(
			List<int> list
		) {
			bw.Write((ushort)list.Count);
		
			foreach (var id in list)
				bw.Write(id);
		}

		public void WriteUserStats(
			User player
		) {
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

		public void WriteBotStats(
			User player
		) {
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

		public void WriteUserPresence(
			User player
		) {
			bw.Write(player.Id);
			bw.WriteOsuString(player.Username);
			bw.Write((byte)(player.TimeZone + 24));
			bw.Write((byte)player.Geoloc.Country.Numeric);
			bw.Write((byte)((int)player.ToBanchoPrivileges() | ((int)player.Status.Mode.AsVanilla() << 5)));
			bw.Write(player.Geoloc.Longitude);
			bw.Write(player.Geoloc.Latitude);
			bw.Write(player.Stats[player.Status.Mode].Rank);
		}

		public void WriteBotPresence(
			User player
		) {
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

		public void WriteOsuString(
			string? data
		) {
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

		public void WriteOsuMessage(
			Message message
		) {
			bw.WriteOsuString(message.Sender);
			bw.WriteOsuString(message.Content);
			bw.WriteOsuString(message.Destination);
			bw.Write(message.SenderId);
		}

		public void WriteOsuChannel(
			Channel channel
		) {
			bw.WriteOsuString(channel.Name);
			bw.WriteOsuString(channel.Description);
			bw.Write((ushort)channel.PlayersCount);
		}

		public void WriteOsuMatch(
			LobbyData lobbyData
		) {
			var match = lobbyData.Match;
		
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

		public void WriteSpectateFrames(
			SpectateFrames frames
		) {
			bw.Write(frames.Extra);
			bw.Write((ushort)frames.ReplayFrames.Count);
		
			foreach (var frame in frames.ReplayFrames)
				bw.WriteReplayFrame(frame);
		
			bw.Write((byte)frames.Action);
			bw.WriteScoreFrame(frames.ScoreFrame);
			bw.Write(frames.Sequence);
		}

		private void WriteScoreFrame(
			ScoreFrame frame
		) {
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

		private void WriteReplayFrame(
			ReplayFrame frame
		) {
			bw.Write(frame.ButtonState);
			bw.Write(frame.TaikoByte);
			bw.Write(frame.MouseX);
			bw.Write(frame.MouseY);
			bw.Write(frame.Time);
		}
	}
}