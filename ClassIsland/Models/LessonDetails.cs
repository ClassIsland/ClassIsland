﻿using System;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class LessonDetails : ObservableRecipient
{
    private Subject _subject = Subject.Fallback;
    private TimeLayoutItem _timeLayoutItem = new TimeLayoutItem();
    private Guid _subjectId = Guid.Empty;

    public Subject Subject
    {
        get => _subject;
        set
        {
            if (Equals(value, _subject)) return;
            _subject = value;
            OnPropertyChanged();
        }
    }

    public TimeLayoutItem TimeLayoutItem
    {
        get => _timeLayoutItem;
        set
        {
            if (Equals(value, _timeLayoutItem)) return;
            _timeLayoutItem = value;
            OnPropertyChanged();
        }
    }

    public Guid SubjectId
    {
        get => _subjectId;
        set
        {
            if (value == _subjectId) return;
            _subjectId = value;
            OnPropertyChanged();
        }
    }
}