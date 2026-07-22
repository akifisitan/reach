namespace Reach.DurableQueue;

public sealed class AlreadyQueuedException() : Exception("Already stored this message");
