﻿ECHO Updating deployment files in Calendar Skill
call ludown parse toluis -c en-us --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\en\general.lu -o %0\..\en -n General --out general.luis
call ludown parse toluis -c en-us --in %0\..\..\CognitiveModels\LUIS\en\calendar.lu -o %0\..\en -n Calendar --out calendar.luis
call ludown parse toluis -c de-de --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\de\general.lu -o %0\..\de -n General --out general.luis
call ludown parse toluis -c de-de --in %0\..\..\CognitiveModels\LUIS\de\calendar.lu -o %0\..\de -n Calendar --out calendar.luis
call ludown parse toluis -c es-es --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\es\general.lu -o %0\..\es -n General --out general.luis
call ludown parse toluis -c es-es --in %0\..\..\CognitiveModels\LUIS\es\calendar.lu -o %0\..\es -n Calendar --out calendar.luis
call ludown parse toluis -c fr-fr --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\fr\general.lu -o %0\..\fr -n General --out general.luis
call ludown parse toluis -c fr-fr --in %0\..\..\CognitiveModels\LUIS\fr\calendar.lu -o %0\..\fr -n Calendar --out calendar.luis
call ludown parse toluis -c it-it --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\it\general.lu -o %0\..\it -n General --out general.luis
call ludown parse toluis -c it-it --in %0\..\..\CognitiveModels\LUIS\it\calendar.lu -o %0\..\it -n Calendar --out calendar.luis
call ludown parse toluis -c zh-cn --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\zh\general.lu -o %0\..\zh -n General --out general.luis
call ludown parse toluis -c zh-cn --in %0\..\..\CognitiveModels\LUIS\zh\calendar.lu -o %0\..\zh -n Calendar --out calendar.luis