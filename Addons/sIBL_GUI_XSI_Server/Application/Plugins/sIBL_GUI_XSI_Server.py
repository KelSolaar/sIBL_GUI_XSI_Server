"""
**sIBL_GUI_XSI_Server.py**

**Platform:**
	Windows, Linux.

**Description:**
	| This module defines the sIBL_GUI_XSI_Server object.

**Others:**

"""

#**********************************************************************************************************************
#***	External imports.
#**********************************************************************************************************************
import SocketServer
import collections
import os
import sys
import thread
from win32com.client import constants as siConstants

#**********************************************************************************************************************
#***	Internal imports.
#**********************************************************************************************************************
__sipath__ not in sys.path and sys.path.append(__sipath__)

from tcpServer import TCPServer

#**********************************************************************************************************************
#***	Module attributes.
#**********************************************************************************************************************
__author__ = "Thomas Mansencal"
__copyright__ = "Copyright (C) 2008 - 2012 - Thomas Mansencal"
__license__ = "GPL V3.0 - http://www.gnu.org/licenses/"
__maintainer__ = "Thomas Mansencal"
__email__ = "thomas.mansencal@gmail.com"
__status__ = "Production"

__all__ = []

#**********************************************************************************************************************
#***	Module classes and definitions.
#**********************************************************************************************************************
class LoggingRequestHandler(SocketServer.BaseRequestHandler):
	def handle(self):
		while True:
			data = self.request.recv(1024)
			if len(data) == 0:
				break
			Application.LogMessage(data)
		return True

class StackDataRequestHandler(SocketServer.BaseRequestHandler):
	def handle(self):
		while True:
			data = self.request.recv(1024)
			if len(data) == 0:
				break
			RuntimeGlobals.requestsStack.append(data)
		return True

class Constants(object):
	defaultAddress = "127.0.0.1"
	defaultPort = 12288
	requestsHandler = StackDataRequestHandler

class RuntimeGlobals(object):
	server = None
	requestsStack = collections.deque()

def XSILoadPlugin(pluginRegistrar):
	pluginRegistrar.Author = __author__
	pluginRegistrar.Name = "sIBL_GUI_XSI_Server"
	pluginRegistrar.URL = "http://www.thomasmansencal.com/"
	pluginRegistrar.Email = "thomas.mansencal@gmail.com"
	pluginRegistrar.Major = 1
	pluginRegistrar.Minor = 0

	pluginRegistrar.RegisterCommand("sIBL_GUI_XSI_Server_start", "sIBL_GUI_XSI_Server_start")
	pluginRegistrar.RegisterCommand("sIBL_GUI_XSI_Server_stop", "sIBL_GUI_XSI_Server_stop")
	pluginRegistrar.RegisterEvent("sIBL_GUI_XSI_Server_startupEvent", siConstants.siOnStartup)	
	pluginRegistrar.RegisterTimerEvent("sIBL_GUI_XSI_Server_timerEvent", 250, 0)

	Application.LogMessage("'{0}' has been loaded!".format(pluginRegistrar.Name))
	return True
	
def XSIUnloadPlugin(pluginRegistrar):
	stopServer()
	Application.LogMessage("'{0}' has been unloaded!".format(pluginRegistrar.Name))
	return True

def sIBL_GUI_XSI_Server_start_Init(context):
	Application.LogMessage("'sIBL_GUI_XSI_Server_start_Init' called!")
	return True

def sIBL_GUI_XSI_Server_start_Execute():
	Application.LogMessage("'sIBL_GUI_XSI_Server_start_Execute' called!")
	startServer()
	return True

def sIBL_GUI_XSI_Server_stop_Init(context):
	Application.LogMessage("'sIBL_GUI_XSI_Server_stop_Init' called!")
	return True

def sIBL_GUI_XSI_Server_stop_Execute():
	Application.LogMessage("'sIBL_GUI_XSI_Server_stop_Execute' called!")
	stopServer()
	return True

def sIBL_GUI_XSI_Server_startupEvent_OnEvent(context):
	Application.LogMessage("'sIBL_GUI_XSI_Server_startupEvent_OnEvent' called!")
	startServer()
	return True

def sIBL_GUI_XSI_Server_timerEvent_OnEvent(context):
	# Application.LogMessage("'sIBL_GUI_XSI_Server_timerEvent' called!")
	processData()
	return False

def startServer():
	RuntimeGlobals.server = TCPServer(Constants.defaultAddress, Constants.defaultPort, Constants.requestsHandler)
	RuntimeGlobals.server.start()
	return True 

def stopServer():
	RuntimeGlobals.server and RuntimeGlobals.server.stop()
	return True 

def processData():
	while RuntimeGlobals.requestsStack:
		data = RuntimeGlobals.requestsStack.popleft().strip()
		if os.path.exists(data):
			Application.ExecuteScript(data)
	return True
