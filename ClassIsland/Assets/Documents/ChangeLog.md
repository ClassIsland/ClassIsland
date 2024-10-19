# 新增功能

1.6 - Himeko 的新增功能。

## 自动化

![图片](https://github.com/user-attachments/assets/8678ead7-a468-4f53-8677-e3c038d805a7)

您可以通过自动化让 ClassIsland 在一些特定的时间节点执行一些特定的操作，比如切换组件配置，运行程序等。

此功能还在早期开发中，欢迎在[ClassIsland/ClassIsland#119](https://github.com/ClassIsland/ClassIsland/issues/119)讨论您的看法和建议。关于此功能的下一步开发方向，请见[此评论](https://github.com/ClassIsland/ClassIsland/issues/119#issuecomment-2408577363)。

***

# 1.5.1.0

1.6 - Himeko

> 1.6 - Himeko 测试版，可能包含未完善和不稳定的功能。

## 新增功能与优化

- **【自动化】自动化初步：** 让 ClassIsland 在一些特定的时间节点执行一些特定的操作，比如切换组件配置，运行程序等 ([#119](https://github.com/ClassIsland/ClassIsland/issues/119))
- 【组件/课表】显示次日课表 ([#288](https://github.com/ClassIsland/ClassIsland/issues/288)) ([#290](https://github.com/ClassIsland/ClassIsland/issues/290))
- 【档案/课表】优化标记换课课程
- 【规则集】优化规则集编辑控件边距
- 【API/课程服务】通过`IPublicLessonService.GetClassPlanByDate`获取特定日期的课表

## Bug 修复

- 【UI】修复托盘菜单弹出位置可能偏移的问题
- 【UI】修复天气图标可能与主题色相近的问题
- 【提醒】修复空时间段没有准备上课提醒的问题
- 【应用设置】修复隐藏窗口选项 CheckBox 间距不一的问题
