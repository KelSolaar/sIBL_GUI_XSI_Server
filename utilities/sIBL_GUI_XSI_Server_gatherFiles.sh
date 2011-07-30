#/bin/bash
echo ----------------------------------------------------------------
echo sIBL_GUI XSI Server - Files Gathering
echo ----------------------------------------------------------------

#! Gathering folder cleanup.
rm -rf ./releases/repository/*

#! Change log gathering.
cp ./releases/Change\ Log.html ./releases/repository/

#! Addon gathering.
cd ./Addons/
zip -r ../releases/repository/sIBL_GUI_XSI_Server.zip sIBL_GUI_XSI_Server.xsiaddon