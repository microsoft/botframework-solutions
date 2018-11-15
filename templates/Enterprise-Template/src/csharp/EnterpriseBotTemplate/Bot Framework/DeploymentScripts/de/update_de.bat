@echo off

ECHO Generating de-de LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\de\general.lu -o %0\..\..\..\DeploymentScripts\de --out general.luis -n General
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\de\dispatch.lu -o %0\..\..\..\DeploymentScripts\de --out dispatch.luis -n Dispatch -i Dispatch -c de-de
call ludown parse toqna  --in %0\..\..\..\CognitiveModels\QnA\de\faq.lu -o %0\..\..\..\DeploymentScripts\de -n faq.qna

@echo on