﻿using System.Net;
using BanchoNET.Models;
using BanchoNET.Models.Dtos;
using BanchoNET.Objects.Privileges;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class ClientRepository(BanchoDbContext dbContext)
{
	public async Task InsertLoginData(
		int playerId,
		IPAddress ip,
		DateTime osuVersion,
		string stream)
	{
		var storedLogin = await dbContext.PlayerLogins
              .FirstOrDefaultAsync(pl => 
                  pl.PlayerId == playerId && 
                  pl.Ip == ip.ToString());
		
		if (storedLogin != null)
		{
			storedLogin.OsuVersion = osuVersion;
			storedLogin.ReleaseStream = stream;
			storedLogin.LoginTime = DateTime.Now;
			await dbContext.SaveChangesAsync();
			return;
		}
		
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
			var hashes = await dbContext.ClientHashes.Include(ch => ch.Player)
			                             .Where(ch => ((Privileges)ch.Player.Privileges & Privileges.Unrestricted) !=
			                                          Privileges.Unrestricted
			                                          && ch.Uninstall == uninstall)
			                             .ToListAsync();

			bannedHashes = hashes.Count;
		}
		else
		{
			var hashes = await dbContext
			                   .ClientHashes.Include(ch => ch.Player)
			                   .Where(ch =>
				                   ((Privileges)ch.Player.Privileges & Privileges.Unrestricted) !=
				                   Privileges.Unrestricted
				                   && ch.Uninstall == uninstall
				                   && ch.Adapters == adapters
				                   && ch.DiskSerial == diskSerial)
			                   .ToListAsync();

			bannedHashes = hashes.Count;
		}

		if (bannedHashes > 0) return true;

		ClientHashesDto? storedHash;

		if (runningUnderWine)
		{
			storedHash = await dbContext.ClientHashes
                 .FirstOrDefaultAsync(ch =>
                     ch.PlayerId == playerId
                     && ch.Uninstall == uninstall);
		}
		else
		{
			storedHash = await dbContext.ClientHashes
                 .FirstOrDefaultAsync(ch =>
                     ch.PlayerId == playerId
                     && ch.Uninstall == uninstall
                     && ch.Adapters == adapters
                     && ch.DiskSerial == diskSerial);
		}

		if (storedHash != null)
		{
			storedHash.LatestTime = DateTime.Now;
			await dbContext.SaveChangesAsync();
			return false;
		}
		
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