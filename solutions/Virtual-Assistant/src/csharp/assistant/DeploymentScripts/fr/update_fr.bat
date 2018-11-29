@echo off

ECHO Generating fr-fr LUIS and QnA Maker models from .lu files ..
call ludown parse toluis -c fr-fr --in %0\..\..\..\..\skills\calendarskill\CognitiveModels\LUIS\fr\calendar.lu -o %0\..\..\..\DeploymentScripts\fr --out calendar.luis -n Calendar
call ludown parse toluis -c fr-fr --in %0\..\..\..\..\skills\emailskill\CognitiveModels\LUIS\fr\email.lu -o %0\..\..\..\DeploymentScripts\fr --out email.luis -n Email
call ludown parse toluis -c fr-fr --in %0\..\..\..\..\skills\todoskill\CognitiveModels\LUIS\fr\todo.lu -o %0\..\..\..\DeploymentScripts\fr --out todo.luis -n ToDo
call ludown parse toluis -c fr-fr --in %0\..\..\..\..\skills\pointofinterestskill\CognitiveModels\LUIS\fr\pointofinterest.lu -o %0\..\..\..\DeploymentScripts\fr --out pointofinterest.luis -n PointOfInterest
call ludown parse toluis -c fr-fr --in %0\..\..\..\CognitiveModels\LUIS\fr\general.lu -o %0\..\..\..\DeploymentScripts\fr --out general.luis -n General
call ludown parse toqna  --in %0\..\..\..\CognitiveModels\QnA\fr\faq.lu -o %0\..\..\..\DeploymentScripts\fr -n faq.qna
call ludown parse toluis -c fr-fr --in %0\..\..\..\CognitiveModels\LUIS\fr\dispatch.lu -o %0\..\..\..\DeploymentScripts\fr --out dispatch.luis -n Dispatch -i Dispatch

@echo on