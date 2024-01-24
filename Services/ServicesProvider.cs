using BanchoNET.Models;

namespace BanchoNET.Services;

public partial class ServicesProvider(BanchoContext context, BanchoSession session)
{
	private readonly BanchoContext _bancho = context;
	private readonly BanchoSession _session = session;
}