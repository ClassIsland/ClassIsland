using System;
using System.Collections.ObjectModel;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Models.Profile;

public class ClassPlansTreeNode
{
    public Guid Guid { get; set; }
    public bool IsGroup { get; set; }
    
    public ReadOnlyObservableCollection<ClassPlansTreeNode>? SubPlans { get; set; }
    public ClassPlan? ClassPlan { get; set; }
}