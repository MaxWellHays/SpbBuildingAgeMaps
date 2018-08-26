using System;
using System.Threading;

namespace SpbBuildingAgeMaps
{
  class TimeDeterrenter
  {
    private int totalAttemptsCount;

    public int TotalAttemptsCount => totalAttemptsCount;

    private long lastSuccessCallAttempt = UtcNowTicks;

    public Action<int> HolderAction { get; set; }
    public TimeSpan ActionTimeInterval { get; set; }

    public TimeDeterrenter(TimeSpan actionTimeInterval)
      : this(null, actionTimeInterval)
    { }

    public TimeDeterrenter(Action<int> holderAction, TimeSpan actionTimeInterval)
    {
      HolderAction = holderAction;
      ActionTimeInterval = actionTimeInterval;
    }

    public (bool successAttempt, int attemptCount) PerformAttempt(int attemptsBatchCount = 1)
    {
      var currentTime = UtcNowTicks;
      var successCallAttemptTime = lastSuccessCallAttempt;
      var currentAttempt = Interlocked.Add(ref totalAttemptsCount, attemptsBatchCount);
      if (currentTime - lastSuccessCallAttempt > ActionTimeInterval.Ticks
        && Interlocked.CompareExchange(ref lastSuccessCallAttempt, currentTime, successCallAttemptTime) == successCallAttemptTime)
      {
        HolderAction?.Invoke(currentAttempt);
        return (true, currentAttempt);
      }
      return (false, currentAttempt);
    }

    private static long UtcNowTicks => DateTime.UtcNow.Ticks;
  }
}
