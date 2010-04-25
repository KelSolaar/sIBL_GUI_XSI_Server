#/bin/bash
echo ----------------------------------------------------------------
echo sIBL_GUI XSI Server - Files Gathering
echo ----------------------------------------------------------------

#! Gathering Folder Cleanup.
rm -rf ./releases/repository/*

#! Change Log Gathering.
cp ./releases/Change\ Log.html ./releases/repository/

#! Addon Gathering.
cd ./Addons/
zip -r ../releases/repository/sIBL_GUI_XSI_Server.zip sIBL_GUI_XSI_Server.xsiaddon