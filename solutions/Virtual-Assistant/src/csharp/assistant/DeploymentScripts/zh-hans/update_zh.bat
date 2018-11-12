@echo off

ECHO Generating zh-hans LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\zh-hans\general.lu -o %0\..\..\..\DeploymentScripts\zh-hans --out general.luis -n General
call ludown parse toluis --in %0\..\..\..\..\skills\calendarskill\CognitiveModels\LUIS\zh-hans\calendar.lu -o %0\..\..\..\DeploymentScripts\zh-hans --out calendar.luis -n Calendar
call ludown parse toluis --in %0\..\..\..\..\skills\emailskill\CognitiveModels\LUIS\zh-hans\email.lu -o %0\..\..\..\DeploymentScripts\zh-hans --out email.luis -n Email
call ludown parse toluis --in %0\..\..\..\..\skills\todoskill\CognitiveModels\LUIS\zh-hans\todo.lu -o %0\..\..\..\DeploymentScripts\zh-hans --out todo.luis -n ToDo
call ludown parse toluis --in %0\..\..\..\..\skills\pointofinterestskill\CognitiveModels\LUIS\zh-hans\pointofinterest.lu -o %0\..\..\..\DeploymentScripts\zh-hans --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna  --in %0\..\..\..\CognitiveModels\QnA\zh-hans\faq.lu -o %0\..\..\..\DeploymentScripts\zh-hans -n faq.qna
call ludown parse toluis --in CognitiveModels\LUIS\zh-hans\dispatch.lu -o %0\..\..\..\DeploymentScripts\zh-hans --out dispatch.luis -n Dispatch -i Dispatch -c zh-cn

@echo on