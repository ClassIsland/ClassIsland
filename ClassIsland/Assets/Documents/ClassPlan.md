# 课表

![1690342618455](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ClassPlan/1690342618455.png)


课表是在某一天具体的课程安排，由课程和触发规则两部分组成。课表的时间安排来源于课表对应的时间表。各个课程表设置相互独立。在开始录入课表前，您应该先录入时间表（详见文章“[时间表](时间表)”）。

## 课程

![1690343130854](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ClassPlan/1690343130854.png)

在课表中，可以给对应时间表中每个上课类型的时间点设置一个上课的科目。科目来源于在【科目】选项卡中定义的科目（详见文章“[科目](科目)”）。

## 触发规则

![1690342708477](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ClassPlan/1690342708477.png)

您还需要设置课表的触发规则。当触发规则全部满足时，该课表会被启用，作为当天的课表，在主界面显示（如图）。

![1690342949494](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ClassPlan/1690342949494.png)

您也可以禁用课表自动启用，这样课表不会自动加载，只能手动启用。

## 临时课表与临时层

![1690343176833](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ClassPlan/1690343176833.png)

如果当天的授课计划有变，需要启用某一天的课表，可以在【临时课表】菜单中临时启用某个课表。该课表会无视触发规则直接启用，在主界面显示。临时课表设置会在应用退出或在第二天到来时清除。您也可以通过点击【清除临时课表】按钮或直接取消勾选启用的临时课表来禁用临时课表。

要进入临时课表菜单，可以点击【课表】选项卡中工具栏上的【临时课表按钮】，也可以点击应用菜单中【加载临时课表】选项。


![1690343249191](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ClassPlan/1690343249191.png)

![1690343286378](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ClassPlan/1690343286378.png)

此外，您可以为一个课表创建临时层。临时层与临时课表相似，会在第二天删除。但您可以单独编辑临时层的课程安排，并不影响原课表。有临时层启用时，将自动覆盖临时课表。

您可以在临时层的课表信息界面中将其转换为普通课表（如图）。

![1707455109839](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ClassPlan/1707455109839.png)

## 换课

打开应用主菜单，点击【换课】按钮即可临时调换课程。以下是换课功能的使用方法。

- 打开主菜单，点击【换课】打开换课界面。

- 选择要调整的课程

![1707454652947](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ClassPlan/1707454652947.png)

- 选择要与刚才选择的课程对调的课程，点击高亮按钮可以切换换课模式。点击【确认换课】以完成换课操作。

![1707454940548](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ClassPlan/1707454940548.png)

![1707454793087](pack://application:,,,/ClassIsland;component/Assets/Documents/image/ClassPlan/1707454793087.png)

完成换课后，应用会创建一个原课表的临时层，并在临时层上调整课程安排，调整只在当日生效。您也可以勾选【永久换课】复选框，直接将换课安排写入原课表。