@echo off

ECHO Generating zh LUIS and QnA Maker models from .lu files ..
call ludown parse toluis -c zh-cn --in %0\..\..\..\..\skills\calendarskill\CognitiveModels\LUIS\zh\calendar.lu -o %0\..\..\..\DeploymentScripts\zh --out calendar.luis -n Calendar
call ludown parse toluis -c zh-cn --in %0\..\..\..\..\skills\emailskill\CognitiveModels\LUIS\zh\email.lu -o %0\..\..\..\DeploymentScripts\zh --out email.luis -n Email
call ludown parse toluis -c zh-cn --in %0\..\..\..\..\skills\todoskill\CognitiveModels\LUIS\zh\todo.lu -o %0\..\..\..\DeploymentScripts\zh --out todo.luis -n ToDo
call ludown parse toluis -c zh-cn --in %0\..\..\..\..\skills\pointofinterestskill\CognitiveModels\LUIS\zh\pointofinterest.lu -o %0\..\..\..\DeploymentScripts\zh --out pointofinterest.luis -n PointOfInterest
call ludown parse toluis -c zh-cn --in %0\..\..\..\CognitiveModels\LUIS\zh\general.lu -o %0\..\..\..\DeploymentScripts\zh --out general.luis -n General
call ludown parse toqna	 --in %0\..\..\..\CognitiveModels\QnA\zh\faq.lu -o %0\..\..\..\DeploymentScripts\zh -n faq.qna
call ludown parse toluis -c zh-cn --in %0\..\..\..\CognitiveModels\LUIS\zh\dispatch.lu -o %0\..\..\..\DeploymentScripts\zh --out dispatch.luis -n Dispatch -i Dispatch

@echo on