using System.Threading.Tasks;

namespace Backlot.Core;

public delegate Task AsyncEventHandler<in TEvent>(object sender, TEvent @event);