namespace Reach.Infra.DurableQueue;

public sealed class AlreadyQueuedException() : Exception("Already stored this message");
