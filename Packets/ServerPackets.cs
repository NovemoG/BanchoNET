using BanchoNET.Objects;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Packets;

public partial class ServerPackets : Packet
{
	public void ProtocolVersion(int version)
	{
		WritePacketData(ServerPacketId.ProtocolVersion, [version]);
	}

	public void UserId(int playerId)
	{
		WritePacketData(ServerPacketId.UserId, [playerId]);
	}

	public void BanchoPrivileges(int privileges)
	{
		WritePacketData(ServerPacketId.Privileges, [privileges]);
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
		
		WritePacketData(ServerPacketId.ChannelInfo, channelData.ToArray());
	}

	public void ChannelInfoEnd()
	{
		WritePacketData(ServerPacketId.ChannelInfoEnd, [null]);
	}

	public void MainMenuIcon(string iconUrl, string onclickUrl)
	{
		WritePacketData(ServerPacketId.MainMenuIcon, [$"{iconUrl}|{onclickUrl}"]);
	}

	public void FriendsList(List<int> friends)
	{
		var friendsData = new List<object> { friends.Count };

		foreach (var id in friends) friendsData.Add(id);
		
		WritePacketData(ServerPacketId.FriendsList, friendsData.ToArray());
	}

	public void SilenceEnd(int delta)
	{
		WritePacketData(ServerPacketId.UserSilenced, [delta]);
	}

	public void UserPresence(Player player)
	{
		WritePacketData(ServerPacketId.UserPresence, 
		[
			player.Id,
			player.Username,
			player.TimeZone,
			(byte)player.Geoloc.Country.Numeric,
			(byte)((int)player.ToBanchoPrivileges() | ((int)player.Status.Mode.AsVanilla() << 5)),
			player.Geoloc.Longitude,
			player.Geoloc.Latitude,
			player.Stats[player.Status.Mode].Rank
		]);
	}

	public void UserStats(Player player)
	{
		//TODO check if pp is over cap and change it to something else

		var modeStats = player.Stats[player.Status.Mode];
		
		WritePacketData(ServerPacketId.UserStats, 
		[
			player.Id,
			(byte)player.Status.Activity,
			player.Status.ActivityDescription,
			player.Status.BeatmapMD5,
			player.Status.CurrentMods,
			player.Status.Mode.AsVanilla(),
			player.Status.BeatmapId,
			modeStats.RankedScore,
			modeStats.Accuracy,
			modeStats.PlayCount,
			modeStats.TotalScore,
			modeStats.Rank,
			modeStats.PP
		]);
	}

	public void SendMessage(Message message)
	{
		WritePacketData(ServerPacketId.SendMessage, 
		[
			message.Sender,
			message.Content,
			message.Destination,
			message.SenderId
		]);
	}

	public void AccountRestricted()
	{
		WritePacketData(ServerPacketId.AccountRestricted, [null]);
	}

	public void Notification(string message)
	{
		WritePacketData(ServerPacketId.Notification, [message]);
	}

	public void RestartServer(int milliseconds)
	{
		WritePacketData(ServerPacketId.Restart, [milliseconds]);
	}

	public void VersionUpdate()
	{
		WritePacketData(ServerPacketId.VersionUpdate, [null]);
	}

	public void Logout(int playerId)
	{
		WritePacketData(ServerPacketId.UserLogout, [playerId, (byte)0]);
	}
}