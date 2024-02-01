using System.Text;
using BanchoNET.Models;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Other;
using BanchoNET.Objects.Player;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Packets.Server;

public class ServerPacket : IDisposable
{
	private readonly MemoryStream _memoryStream = new();

	public void ProtocolVersion(int version)
	{
		_memoryStream.WritePacket(ServerPackets.PROTOCOL_VERSION, [version]);
	}

	public void UserId(int playerId)
	{
		_memoryStream.WritePacket(ServerPackets.USER_ID, [playerId]);
	}

	public void BanchoPrivileges(int privileges)
	{
		_memoryStream.WritePacket(ServerPackets.PRIVILEGES, [privileges]);
	}

	public void WelcomeMessage(string message)
	{
		_memoryStream.Write(Encoding.UTF8.GetBytes(message));
	}

	public void ChannelInfo(List<Channel> channels)
	{
		var channelData = new List<object>();
		
		foreach (var channel in channels)
		{
			channelData.Add(channel.Name);
			channelData.Add(channel.Description);
			channelData.Add(channel.Players.Count);
		}
		
		_memoryStream.WritePacket(ServerPackets.CHANNEL_INFO, channelData.ToArray());
	}

	public void ChannelInfoEnd()
	{
		_memoryStream.WritePacket(ServerPackets.CHANNEL_INFO_END, [null]);
	}

	public void MainMenuIcon(string iconUrl, string onclickUrl)
	{
		_memoryStream.WritePacket(ServerPackets.MAIN_MENU_ICON, [$"{iconUrl}|{onclickUrl}"]);
	}

	public void FriendsList(List<int> friends)
	{
		var friendsData = new List<object> { friends.Count };

		foreach (var id in friends)
		{
			friendsData.Add(id);
		}
		
		_memoryStream.WritePacket(ServerPackets.FRIENDS_LIST, friendsData.ToArray());
	}

	public void SilenceEnd(int delta)
	{
		_memoryStream.WritePacket(ServerPackets.USER_SILENCED, [delta]);
	}

	public void UserPresence(Player player)
	{
		var presence = new PlayerPresence
		{
			PlayerId = player.Id,
			Username = player.Username,
			TimeZone = player.TimeZone, //TODO +24 maybe?
			CountryId = 3, //TODO get country id from country dictionary
			Privileges = (byte)(player.Privileges | (0 << 5)), //TODO get current gamemode (vanilla osu)
			Longitude = player.Longitude,
			Latitude = player.Latitude,
			Rank = player.Rank
		};

		_memoryStream.WritePacket(ServerPackets.USER_PRESENCE,
		[
			presence.PlayerId,
			presence.Username,
			presence.TimeZone,
			presence.CountryId,
			presence.Privileges,
			presence.Longitude,
			presence.Latitude,
			presence.Rank
		]);
	}

	public void UserStats(Player player)
	{
		//TODO get user gamemode stats
		var stats = new PlayerStats
		{
			PlayerId = player.Id,
			Activity = 0,
			ActivityDescription = "",
			BeatmapMD5 = "",
			CurrentMods = 0,
			Mode = 0,
			BeatmapId = 0,
			RankedScore = 0,
			Accuracy = 0.0f,
			PlayCount = 0,
			TotalScore = 0,
			Rank = 1,
			PP = 0
		};
		
		//TODO check if pp is over cap and change it to something else

		_memoryStream.WritePacket(ServerPackets.USER_STATS,
		[
			stats.PlayerId,
			stats.Activity,
			stats.ActivityDescription,
			stats.BeatmapMD5,
			stats.CurrentMods,
			stats.Mode,
			stats.BeatmapId,
			stats.RankedScore,
			stats.Accuracy,
			stats.PlayCount,
			stats.TotalScore,
			stats.Rank,
			stats.PP,
		]);
	}

	public void SendMessage(Message message)
	{
		_memoryStream.WritePacket(ServerPackets.SEND_MESSAGE,
		[
			message.Sender,
			message.Content,
			message.Destination,
			message.SenderId
		]);
	}

	public void AccountRestricted()
	{
		_memoryStream.WritePacket(ServerPackets.ACCOUNT_RESTRICTED, [null]);
	}
	
	public async Task WriteToResponse(HttpResponse response)
	{
		await response.StartAsync();
		await _memoryStream.CopyToAsync(response.Body);
		await response.CompleteAsync();
		
		await _memoryStream.FlushAsync();
	}

	public void Dispose()
	{
		_memoryStream.Dispose();
	}
}