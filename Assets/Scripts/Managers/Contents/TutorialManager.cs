
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Define;

[System.Serializable]
public class TutorialInfo
{
    public string _tutorialId;
    public string _tutorialName;
    public string _tutorialDescription;
}

public class TutorialManager
{
    private Dictionary<string, TutorialInfo> _tutorials = new Dictionary<string, TutorialInfo>();
    private Dictionary<TaskCompletionSource<bool>, string> _tutorialTasks = new Dictionary<TaskCompletionSource<bool>, string>();

    public event Action<string> OnTutorialStarted;
    public event Action<string> OnTutorialEnded;
    public event Action<string> OnTutorialProgressUpdated;
    public event Action<string> OnTutorialCompleted;
    public event Action<string> OnTutorialFailed;

    public void Init()
    {
    }

    public void Clear()
    {
    }
}