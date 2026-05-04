using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Lazer.Spectator.Frames;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;

namespace BanchoNET.Core.Utils.Replays;

public static class ReplaySerializer
{
	public const int EARLY_VERSION_TIMING_OFFSET = 24;
	public const int LATEST_VERSION = 30000016;

	public static void Serialize(
		ApiScore score,
		List<LegacyReplayFrame> frames,
		Beatmap beatmap
	) {
		using var fs = new FileStream(
			Storage.GetReplayPath(score.Id),
			FileMode.Create,
			FileAccess.Write,
			FileShare.None,
			bufferSize: 4096
		);
		using var bw = new BinaryWriter(fs, Encoding.ASCII);
		
		bw.Write((byte)score.RulesetId);
		bw.Write(LATEST_VERSION);
		bw.WriteString(beatmap.Checksum);
		bw.WriteString(score.User!.Username);
		bw.WriteString($"lazer-{score.User.Username}-{score.EndedAt}".CreateMD5());
		bw.Write((ushort)score.GetCount300());
		bw.Write((ushort)score.GetCount100());
		bw.Write((ushort)score.GetCount50());
		bw.Write((ushort)score.GetCountGeki());
		bw.Write((ushort)score.GetCountKatu());
		bw.Write((ushort)score.GetCountMiss());
		bw.Write(score.TotalScore);
		bw.Write((ushort)score.MaxCombo);
		bw.Write(score.IsPerfectCombo);
		bw.Write((int)score.Mods.ToLegacyMods());
		
		bw.WriteHealthGraph();
		bw.WriteDate(score.EndedAt.DateTime);
		bw.WriteInputData(frames);
		bw.Write(score.LegacyScoreId ?? -1);
		bw.WriteAdditionalScoreData(score);
	}

	extension(
		BinaryWriter bw
	) {
		private void WriteString(
			string text
		) {
			if (text == string.Empty)
			{
				bw.Write((byte)0);
			}
			else
			{
				bw.Write((byte)11);
				bw.Write(text);
			}
		}

		private void WriteDate(
			DateTime date
		) {
			bw.Write(date.ToUniversalTime().Ticks);
		}

		private void WriteHealthGraph() {
			bw.WriteString(string.Empty);
		}

		private void WriteAdditionalScoreData(
			ApiScore score
		) {
			var serializedData = JsonSerializer.Serialize(new LegacyReplaySoloScoreInfo
			{
				OnlineID = score.Id,
				Mods = score.Mods,
				Statistics = score.Statistics.Where(kvp => kvp.Value != 0).ToDictionary(),
				MaximumStatistics = score.MaximumStatistics.Where(kvp => kvp.Value != 0).ToDictionary(),
				ClientVersion = "",
				Rank = score.Rank,
				UserID = score.User!.Id,
				TotalScoreWithoutMods = score.TotalScoreWithoutMods > 0 ? score.TotalScoreWithoutMods : null,
				Pauses = score.Pauses
			}, SnakeCaseNamingPolicy.ReplayOptions);
		
			bw.WriteCompressedString(serializedData);
		}

		private void WriteInputData(
			List<LegacyReplayFrame> inputData
		) {
			var sb = new StringBuilder();
		
			//TODO beatmaps that have version <5 should have -24ms applied to every time value of each frame
			var lastTime = 0;
		
			foreach (var data in inputData)
			{
				var time = (int)Math.Round(data.Time);
				sb.Append($"{time - lastTime}|{data.MouseX ?? 0}|{data.MouseY ?? 0}|{(int)data.ButtonState},");
				lastTime = time;
			}

			sb.Append("-12345|0|0|0");
		
			bw.WriteCompressedString(sb.ToString());
		}

		private void WriteCompressedString(
			string data
		) {
			var rawBytes = Encoding.ASCII.GetBytes(data);
			using var ms = new MemoryStream();
		
			ms.Write(rawBytes, 0, rawBytes.Length);
		
			var encoded = LZMAHelper.Compress(ms);
			var rawBytesCompressed = new byte[encoded.Length];
			encoded.ReadExactly(rawBytesCompressed, 0, rawBytesCompressed.Length);
		
			bw.Write(rawBytesCompressed.Length);
			bw.Write(rawBytesCompressed);
		}
	}

	public static ApiScore Deserialize(
		long scoreId
	) {
		var score = new ApiScore();
		
		using var fs = new FileStream(
			Storage.GetReplayPath(scoreId),
			FileMode.Open,
			FileAccess.Read,
			FileShare.ReadWrite,
			bufferSize: 4096
		);
		using var br = new BinaryReader(fs, Encoding.ASCII);
		
		// read data
		
		return score;
	}

	extension(
		BinaryReader br
	) {
		private string ReadReplayString() {
			var readStringByte = br.ReadByte();
			return readStringByte == 11 ? br.ReadString() : string.Empty;
		}

		private DateTime ReadReplayDate() {
			return new DateTime(br.ReadInt64());
		}

		private List<HealthData> ReadReplayHealthData() {
			var data = br.ReadReplayString().Split('|');

			var healthData = new List<HealthData>();
			for (int i = 1; i < data.Length; i++)
			{
				var values = data[i].Split(',');
			
				var hpValue = float.Parse(values[0]);
				if (!long.TryParse(values[1], out var millis)) millis = 0;
			
				healthData.Add(new HealthData(hpValue, millis));
			}

			return healthData;
		}

		private List<LegacyReplayFrame> ReadReplayInputData(
			int bytesLength,
			out int seed
		) {
			seed = 0;
			var byteData = br.ReadBytes(bytesLength);
		
			var decompressedData = Encoding.ASCII.GetString((ReadOnlySpan<byte>)LZMAHelper.Decompress(byteData));
			var inputData = decompressedData.Split(',');
		
			var returnData = new List<LegacyReplayFrame>();
			foreach (var data in inputData)
			{
				if (data == string.Empty) break;
			
				var values = data.Split('|');
			
				var millis = long.Parse(values[0]);
				if (millis == -12345)
				{
					seed = int.Parse(values[3]);
					break;
				}
			
				var xCord = float.Parse(values[1]);
				var yCord = float.Parse(values[2]);
				var button = int.Parse(values[3]);
			
				returnData.Add(new LegacyReplayFrame(millis, xCord, yCord, (ReplayButtonState)button));
			}

			return returnData;
		}
	}

	[Serializable]
	private class LegacyReplaySoloScoreInfo
	{
		[JsonPropertyName("online_id")]
		public long OnlineID { get; set; } = -1;

		[JsonPropertyName("mods")]
		public Mod[] Mods { get; set; } = [];

		[JsonPropertyName("statistics")]
		public Dictionary<HitResult, int> Statistics { get; set; } = new();

		[JsonPropertyName("maximum_statistics")]
		public Dictionary<HitResult, int> MaximumStatistics { get; set; } = new();

		[JsonPropertyName("client_version")]
		public string ClientVersion = string.Empty;

		[JsonPropertyName("rank")]
		public string? Rank;

		[JsonPropertyName("user_id")]
		public int UserID = -1;

		[JsonPropertyName("total_score_without_mods")]
		public long? TotalScoreWithoutMods { get; set; }

		[JsonPropertyName("pauses")]
		public int[] Pauses { get; set; } = [];
	}
}