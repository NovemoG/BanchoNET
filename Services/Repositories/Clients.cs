using System.Net;
using BanchoNET.Abstractions.Repositories;
using BanchoNET.Models;
using BanchoNET.Models.Dtos;
using BanchoNET.Objects.Privileges;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class ClientsRepository(BanchoDbContext dbContext) : IClientsRepository
{
	public async Task InsertLoginData(
		int playerId,
		IPAddress ip,
		DateTime osuVersion,
		string stream)
	{
		var hasLoginData = await dbContext.PlayerLogins.Where(pl =>
				pl.PlayerId == playerId
				&& pl.Ip == ip.ToString())
			.ExecuteUpdateAsync(p =>
				p.SetProperty(x => x.OsuVersion, osuVersion)
					.SetProperty(x => x.ReleaseStream, stream)
					.SetProperty(x => x.LoginTime, DateTime.Now));

		if (hasLoginData > 0) return;
		
		await dbContext.PlayerLogins.AddAsync(new LoginDto
		{
			PlayerId = playerId,
			Ip = ip.ToString(),
			OsuVersion = osuVersion,
			ReleaseStream = stream,
			LoginTime = DateTime.Now
		});
		await dbContext.SaveChangesAsync();
	}

	public async Task<bool> TryInsertClientHashes(
		int playerId,
		string osuPath,
		string adapters,
		string uninstall,
		string diskSerial,
		bool runningUnderWine = false)
	{
		int bannedHashes;
		if (runningUnderWine)
		{
			var hashes = await dbContext
				.ClientHashes.Include(ch => ch.Player)
				.Where(ch =>
					((PlayerPrivileges)ch.Player.Privileges & PlayerPrivileges.Unrestricted) !=
					PlayerPrivileges.Unrestricted
					&& ch.Uninstall == uninstall)
				.ToListAsync();

			bannedHashes = hashes.Count;
		}
		else
		{
			var hashes = await dbContext
				.ClientHashes.Include(ch => ch.Player)
				.Where(ch =>
					((PlayerPrivileges)ch.Player.Privileges & PlayerPrivileges.Unrestricted) !=
					PlayerPrivileges.Unrestricted
					&& ch.Uninstall == uninstall
					&& ch.Adapters == adapters
					&& ch.DiskSerial == diskSerial)
				.ToListAsync();

			bannedHashes = hashes.Count;
		}
		if (bannedHashes > 0) return true;
		
		var hasClientHash = runningUnderWine
			? await dbContext.ClientHashes.Where(ch =>
					ch.PlayerId == playerId
					&& ch.Uninstall == uninstall)
				.ExecuteUpdateAsync(p =>
					p.SetProperty(h => h.LatestTime, DateTime.Now))
			: await dbContext.ClientHashes.Where(ch =>
					ch.PlayerId == playerId
					&& ch.Uninstall == uninstall
					&& ch.Adapters == adapters
					&& ch.DiskSerial == diskSerial)
				.ExecuteUpdateAsync(p =>
					p.SetProperty(h => h.LatestTime, DateTime.Now));
		
		if (hasClientHash > 0) return false;
		
		await dbContext.ClientHashes.AddAsync(new ClientHashesDto
		{
			PlayerId = playerId,
			OsuPath = osuPath,
			Adapters = adapters,
			Uninstall = uninstall,
			DiskSerial = diskSerial,
			LatestTime = DateTime.Now
		});
		await dbContext.SaveChangesAsync();
		return false;
	}
}