using System.Net;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Privileges;
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
					.SetProperty(x => x.LoginTime, DateTime.UtcNow));

		if (hasLoginData > 0) return;
		
		await dbContext.PlayerLogins.AddAsync(new LoginDto
		{
			PlayerId = playerId,
			Ip = ip.ToString(),
			OsuVersion = osuVersion,
			ReleaseStream = stream,
			LoginTime = DateTime.UtcNow
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
					p.SetProperty(h => h.LatestTime, DateTime.UtcNow))
			: await dbContext.ClientHashes.Where(ch =>
					ch.PlayerId == playerId
					&& ch.Uninstall == uninstall
					&& ch.Adapters == adapters
					&& ch.DiskSerial == diskSerial)
				.ExecuteUpdateAsync(p =>
					p.SetProperty(h => h.LatestTime, DateTime.UtcNow));
		
		if (hasClientHash > 0) return false;
		
		await dbContext.ClientHashes.AddAsync(new ClientHashesDto
		{
			PlayerId = playerId,
			OsuPath = osuPath,
			Adapters = adapters,
			Uninstall = uninstall,
			DiskSerial = diskSerial,
			LatestTime = DateTime.UtcNow
		});
		await dbContext.SaveChangesAsync();
		return false;
	}
}