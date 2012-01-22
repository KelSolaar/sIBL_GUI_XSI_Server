"""
**tcpServer.py**

**Platform:**
	Windows, Linux.

**Description:**
	This module defines the :class:`TCPServer` and :class:`RequestHandler` classes.

**Others:**

"""

#**********************************************************************************************************************
#***	External imports.
#**********************************************************************************************************************
import SocketServer
import sys
import threading


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
class DefaultRequestHandler(SocketServer.BaseRequestHandler):

	def handle(self):
		while True:
			data = self.request.recv(1024)
			if len(data) == 0:
				break
			sys.stdout.write(data)
		return True

class TCPServer(object):

	def __init__(self, address, port, handler=DefaultRequestHandler):
		self.__address = None
		self.address = address
		self.__port = None
		self.port = port
		self.__handler = None
		self.handler = handler

		self.__server = SocketServer.TCPServer((address, port), handler)
		self.__worker = None

	#******************************************************************************************************************
	#***	Attributes properties.
	#******************************************************************************************************************
	@property
	def address(self):
		return self.__address

	@address.setter
	def address(self, value):
		if value is not None:
			assert type(value) in (str, unicode), "'{0}' attribute: '{1}' type is not 'str' or 'unicode'!".format(
			"address", value)
		self.__address = value

	@address.deleter
	def address(self):
		raise Exception("{0} | '{1}' attribute is not deletable!".format(self.__class__.__name__, "address"))

	@property
	def port(self):
		return self.__port

	@port.setter
	def port(self, value):
		if value is not None:
			assert type(value) is int, "'{0}' attribute: '{1}' type is not 'int'!".format(
			"port", value)
		self.__port = value

	@port.deleter
	def port(self):
		raise Exception("{0} | '{1}' attribute is not deletable!".format(self.__class__.__name__, "port"))

	@property
	def handler(self):
		return self.__handler

	@handler.setter
	def handler(self, value):
		if value is not None:
			assert issubclass(value, SocketServer.BaseRequestHandler), \
			"'{0}' attribute: '{1}' is not 'SocketServer.BaseRequestHandler' subclass!".format("handler", value)
		self.__handler = value

	@handler.deleter
	def handler(self):
		raise Exception("{0} | '{1}' attribute is not deletable!".format(self.__class__.__name__, "handler"))

	#******************************************************************************************************************
	#***	Class methods.
	#******************************************************************************************************************
	def start(self):
		self.__worker = threading.Thread(target=self.__server.serve_forever)
		self.__worker.setDaemon(True)
		self.__worker.start()
	
	def stop(self):
		self.__server.socket.close()
		self.__server.shutdown()
