using System.Collections.Concurrent;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services;

public sealed class BanchoSession : IBanchoSession
{
	#region Other
	
	private readonly ConcurrentDictionary<string, string> _passwordHashes = [];

	#endregion

	public void ClearPasswordsCache() => _passwordHashes.Clear();
	
	public void InsertPasswordHash(string passwordMD5, string passwordHash)
	{
		_passwordHashes.TryAdd(passwordHash, passwordMD5);
	}
	
	public bool CheckHashes(string passwordMD5, string passwordHash)
	{
		if (_passwordHashes.TryGetValue(passwordHash, out var md5))
			return md5 == passwordMD5;

		if (!BCrypt.Net.BCrypt.Verify(passwordMD5, passwordHash))
			return false;
		
		_passwordHashes.TryAdd(passwordHash, passwordMD5);
		return true;
	}
}