@echo off

ECHO Generating fr-fr LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\fr\general.lu -o %0\..\..\..\DeploymentScripts\fr -n General --out general.luis
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\fr\dispatch.lu -o %0\..\..\..\DeploymentScripts\fr --out dispatch.luis -n Dispatch -i Dispatch
call ludown parse toqna  --in %0\..\..\..\CognitiveModels\QnA\fr\faq.lu -o %0\..\..\..\DeploymentScripts\fr -n faq.qna

@echo on