@echo off

ECHO Generating it-it LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\it\general.lu -o %0\..\..\..\DeploymentScripts\it -n General --out general.luis
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\it\dispatch.lu -o %0\..\..\..\DeploymentScripts\it --out dispatch.luis -n Dispatch -i Dispatch
call ludown parse toqna  --in %0\..\..\..\CognitiveModels\QnA\it\faq.lu -o %0\..\..\..\DeploymentScripts\it -n faq.qna

@echo on