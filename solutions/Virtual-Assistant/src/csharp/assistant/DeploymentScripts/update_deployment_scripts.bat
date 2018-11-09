@echo off

ECHO Generating en-us LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in ../CognitiveModels/LUIS/en/general.lu -o en -n General --out general.luis
call ludown parse toluis --in ../../skills/calendarskill/CognitiveModels/LUIS/en/calendar.lu -o en -n Calendar --out calendar.luis
call ludown parse toluis --in ../../skills/emailskill/CognitiveModels/LUIS/en/email.lu -o en -n Email --out email.luis -n Email
call ludown parse toluis --in ../../skills/pointofinterestskill/CognitiveModels/LUIS/en/pointofinterest.lu -o en -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in ../../skills/todoskill/CognitiveModels/LUIS/en/todo.lu -o en -n ToDo --out todo.luis
call ludown parse toqna --in ../CognitiveModels/QnA/en/faq.lu -o en -n faq.qna
call ludown parse toluis --in ../CognitiveModels/LUIS/en/dispatch.lu -o en --out dispatch.luis -n Dispatch -i Dispatch

ECHO Generating de-de LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in ../CognitiveModels/LUIS/de/general.lu -o de --out general.luis -n General
call ludown parse toluis --in ../../skills/calendarskill/CognitiveModels/LUIS/de/calendar.lu -o de --out calendar.luis -n Calendar
call ludown parse toluis --in ../../skills/emailskill/CognitiveModels/LUIS/de/email.lu -o de --out email.luis -n Email
call ludown parse toluis --in ../../skills/todoskill/CognitiveModels/LUIS/de/todo.lu -o de --out todo.luis -n ToDo
call ludown parse toluis --in ../../skills/pointofinterestskill/CognitiveModels/LUIS/de/pointofinterest.lu -o de --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in ../CognitiveModels/QnA/de/faq.lu -o de -n faq.qna
call ludown parse toluis --in ../CognitiveModels/LUIS/de/dispatch.lu -o de --out dispatch.luis -n Dispatch -i Dispatch -c de-de

ECHO Generating es-es LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in ../CognitiveModels/LUIS/es/general.lu -o es --out general.luis -n General
call ludown parse toluis --in ../../skills/calendarskill/CognitiveModels/LUIS/es/calendar.lu -o es --out calendar.luis -n Calendar
call ludown parse toluis --in ../../skills/emailskill/CognitiveModels/LUIS/es/email.lu -o es --out email.luis -n Email
call ludown parse toluis --in ../../skills/todoskill/CognitiveModels/LUIS/es/todo.lu -o es --out todo.luis -n ToDo
call ludown parse toluis --in ../../skills/pointofinterestskill/CognitiveModels/LUIS/es/pointofinterest.lu -o es --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in ../CognitiveModels/QnA/es/faq.lu -o es -n faq.qna
call ludown parse toluis --in ../CognitiveModels/LUIS/es/dispatch.lu -o es --out dispatch.luis -n Dispatch -i Dispatch -c es-es

ECHO Generating fr-fr LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in ../CognitiveModels/LUIS/fr/general.lu -o fr --out general.luis -n General
call ludown parse toluis --in ../../skills/calendarskill/CognitiveModels/LUIS/fr/calendar.lu -o fr --out calendar.luis -n Calendar
call ludown parse toluis --in ../../skills/emailskill/CognitiveModels/LUIS/fr/email.lu -o fr --out email.luis -n Email
call ludown parse toluis --in ../../skills/todoskill/CognitiveModels/LUIS/fr/todo.lu -o fr --out todo.luis -n ToDo
call ludown parse toluis --in ../../skills/pointofinterestskill/CognitiveModels/LUIS/fr/pointofinterest.lu -o fr --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in ../CognitiveModels/QnA/fr/faq.lu -o fr -n faq.qna
call ludown parse toluis --in ../CognitiveModels/LUIS/fr/dispatch.lu -o fr --out dispatch.luis -n Dispatch -i Dispatch -c fr-fr

ECHO Generating it-it LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in ../CognitiveModels/LUIS/it/general.lu -o it --out general.luis -n General
call ludown parse toluis --in ../../skills/calendarskill/CognitiveModels/LUIS/it/calendar.lu -o it --out calendar.luis -n Calendar
call ludown parse toluis --in ../../skills/emailskill/CognitiveModels/LUIS/it/email.lu -o it --out email.luis -n Email
call ludown parse toluis --in ../../skills/todoskill/CognitiveModels/LUIS/it/todo.lu -o it --out todo.luis -n ToDo
call ludown parse toluis --in ../../skills/pointofinterestskill/CognitiveModels/LUIS/it/pointofinterest.lu -o it --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in ../CognitiveModels/QnA/it/faq.lu -o it -n faq.qna
call ludown parse toluis --in ../CognitiveModels/LUIS/it/dispatch.lu -o it --out dispatch.luis -n Dispatch -i Dispatch -c it-it

ECHO Generating zh-hans LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in ../CognitiveModels/LUIS/zh-hans/general.lu -o zh-hans --out general.luis -n General
call ludown parse toluis --in ../../skills/calendarskill/CognitiveModels/LUIS/zh-hans/calendar.lu -o zh-hans --out calendar.luis -n Calendar
call ludown parse toluis --in ../../skills/emailskill/CognitiveModels/LUIS/zh-hans/email.lu -o zh-hans --out email.luis -n Email
call ludown parse toluis --in ../../skills/todoskill/CognitiveModels/LUIS/zh-hans/todo.lu -o zh-hans --out todo.luis -n ToDo
call ludown parse toluis --in ../../skills/pointofinterestskill/CognitiveModels/LUIS/zh-hans/pointofinterest.lu -o zh-hans --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in ../CognitiveModels/QnA/zh-hans/faq.lu -o zh-hans -n faq.qna
call ludown parse toluis --in ../CognitiveModels/LUIS/zh-hans/dispatch.lu -o zh-hans --out dispatch.luis -n Dispatch -i Dispatch -c zh-cn

ECHO Updating deployment files in Calendar Skill
call ludown parse toluis --in ../CognitiveModels/LUIS/en/general.lu -o ../../skills/calendarskill/DeploymentScripts/en/ -n General --out general.luis
call ludown parse toluis --in ../../skills/calendarskill/CognitiveModels/LUIS/en/calendar.lu -o ../../skills/calendarskill/DeploymentScripts/en -n Calendar --out calendar.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/de/general.lu -o ../../skills/calendarskill/DeploymentScripts/de -n General --out general.luis
call ludown parse toluis --in ../../skills/calendarskill/CognitiveModels/LUIS/de/calendar.lu -o ../../skills/calendarskill/DeploymentScripts/de -n Calendar --out calendar.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/es/general.lu -o ../../skills/calendarskill/DeploymentScripts/es -n General --out general.luis
call ludown parse toluis --in ../../skills/calendarskill/CognitiveModels/LUIS/es/calendar.lu -o ../../skills/calendarskill/DeploymentScripts/es -n Calendar --out calendar.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/fr/general.lu -o ../../skills/calendarskill/DeploymentScripts/fr -n General --out general.luis
call ludown parse toluis --in ../../skills/calendarskill/CognitiveModels/LUIS/fr/calendar.lu -o ../../skills/calendarskill/DeploymentScripts/fr -n Calendar --out calendar.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/it/general.lu -o ../../skills/calendarskill/DeploymentScripts/it -n General --out general.luis
call ludown parse toluis --in ../../skills/calendarskill/CognitiveModels/LUIS/it/calendar.lu -o ../../skills/calendarskill/DeploymentScripts/it -n Calendar --out calendar.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/zh-hans/general.lu -o ../../skills/calendarskill/DeploymentScripts/zh-hans -n General --out general.luis
call ludown parse toluis --in ../../skills/calendarskill/CognitiveModels/LUIS/zh-hans/calendar.lu -o ../../skills/calendarskill/DeploymentScripts/zh-hans -n Calendar --out calendar.luis

ECHO Updating deployment files in Email Skill
call ludown parse toluis --in ../CognitiveModels/LUIS/en/general.lu -o ../../skills/emailskill/DeploymentScripts/en/ -n General --out general.luis
call ludown parse toluis --in ../../skills/emailskill/CognitiveModels/LUIS/en/email.lu -o ../../skills/emailskill/DeploymentScripts/en -n Email --out email.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/de/general.lu -o ../../skills/emailskill/DeploymentScripts/de -n General --out general.luis
call ludown parse toluis --in ../../skills/emailskill/CognitiveModels/LUIS/de/email.lu -o ../../skills/emailskill/DeploymentScripts/de -n Email --out email.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/es/general.lu -o ../../skills/emailskill/DeploymentScripts/es -n General --out general.luis
call ludown parse toluis --in ../../skills/emailskill/CognitiveModels/LUIS/es/email.lu -o ../../skills/emailskill/DeploymentScripts/es -n Email --out email.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/fr/general.lu -o ../../skills/emailskill/DeploymentScripts/fr -n General --out general.luis
call ludown parse toluis --in ../../skills/emailskill/CognitiveModels/LUIS/fr/email.lu -o ../../skills/emailskill/DeploymentScripts/fr -n Email --out email.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/it/general.lu -o ../../skills/emailskill/DeploymentScripts/it -n General --out general.luis
call ludown parse toluis --in ../../skills/emailskill/CognitiveModels/LUIS/it/email.lu -o ../../skills/emailskill/DeploymentScripts/it -n Email --out email.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/zh-hans/general.lu -o ../../skills/emailskill/DeploymentScripts/zh-hans -n General --out general.luis
call ludown parse toluis --in ../../skills/emailskill/CognitiveModels/LUIS/zh-hans/email.lu -o ../../skills/emailskill/DeploymentScripts/zh-hans -n Email --out email.luis

ECHO Updating deployment files in ToDo Skill
call ludown parse toluis --in ../CognitiveModels/LUIS/en/general.lu -o ../../skills/todoskill/DeploymentScripts/en/ -n General --out general.luis
call ludown parse toluis --in ../../skills/todoskill/CognitiveModels/LUIS/en/todo.lu -o ../../skills/todoskill/DeploymentScripts/en -n ToDo --out todo.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/de/general.lu -o ../../skills/todoskill/DeploymentScripts/de -n General --out general.luis
call ludown parse toluis --in ../../skills/todoskill/CognitiveModels/LUIS/de/todo.lu -o ../../skills/todoskill/DeploymentScripts/de -n ToDo --out todo.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/es/general.lu -o ../../skills/todoskill/DeploymentScripts/es -n General --out general.luis
call ludown parse toluis --in ../../skills/todoskill/CognitiveModels/LUIS/es/todo.lu -o ../../skills/todoskill/DeploymentScripts/es -n ToDo --out todo.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/fr/general.lu -o ../../skills/todoskill/DeploymentScripts/fr -n General --out general.luis
call ludown parse toluis --in ../../skills/todoskill/CognitiveModels/LUIS/fr/todo.lu -o ../../skills/todoskill/DeploymentScripts/fr -n ToDo --out todo.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/it/general.lu -o ../../skills/todoskill/DeploymentScripts/it -n General --out general.luis
call ludown parse toluis --in ../../skills/todoskill/CognitiveModels/LUIS/it/todo.lu -o ../../skills/todoskill/DeploymentScripts/it -n ToDo --out todo.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/zh-hans/general.lu -o ../../skills/todoskill/DeploymentScripts/zh-hans -n General --out general.luis
call ludown parse toluis --in ../../skills/todoskill/CognitiveModels/LUIS/zh-hans/todo.lu -o ../../skills/todoskill/DeploymentScripts/zh-hans -n ToDo --out todo.luis

ECHO Updating deployment files in PointOfInterest Skill
call ludown parse toluis --in ../CognitiveModels/LUIS/en/general.lu -o ../../skills/pointofinterestskill/DeploymentScripts/en/ -n General --out general.luis
call ludown parse toluis --in ../../skills/pointofinterestskill/CognitiveModels/LUIS/en/pointofinterest.lu -o ../../skills/pointofinterestskill/DeploymentScripts/en -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/de/general.lu -o ../../skills/pointofinterestskill/DeploymentScripts/de -n General --out general.luis
call ludown parse toluis --in ../../skills/pointofinterestskill/CognitiveModels/LUIS/de/pointofinterest.lu -o ../../skills/pointofinterestskill/DeploymentScripts/de -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/es/general.lu -o ../../skills/pointofinterestskill/DeploymentScripts/es -n General --out general.luis
call ludown parse toluis --in ../../skills/pointofinterestskill/CognitiveModels/LUIS/es/pointofinterest.lu -o ../../skills/pointofinterestskill/DeploymentScripts/es -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/fr/general.lu -o ../../skills/pointofinterestskill/DeploymentScripts/fr -n General --out general.luis
call ludown parse toluis --in ../../skills/pointofinterestskill/CognitiveModels/LUIS/fr/pointofinterest.lu -o ../../skills/pointofinterestskill/DeploymentScripts/fr -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/it/general.lu -o ../../skills/pointofinterestskill/DeploymentScripts/it -n General --out general.luis
call ludown parse toluis --in ../../skills/pointofinterestskill/CognitiveModels/LUIS/it/pointofinterest.lu -o ../../skills/pointofinterestskill/DeploymentScripts/it -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in ../CognitiveModels/LUIS/zh-hans/general.lu -o ../../skills/pointofinterestskill/DeploymentScripts/zh-hans -n General --out general.luis
call ludown parse toluis --in ../../skills/pointofinterestskill/CognitiveModels/LUIS/zh-hans/pointofinterest.lu -o ../../skills/pointofinterestskill/DeploymentScripts/zh-hans -n PointOfInterest --out pointofinterest.luis

@echo on