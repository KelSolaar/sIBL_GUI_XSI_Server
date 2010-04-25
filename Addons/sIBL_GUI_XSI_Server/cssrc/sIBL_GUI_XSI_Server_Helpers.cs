using Softimage.XSIOM; // XSI object model
using Softimage.XSIMath;
using Softimage.XSIUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace XSI.Helpers
{
	public class Base
	{
		CXSIApplicationClass m_xsi;
		CXSIFactoryClass m_fact;
		CXSIUtilsClass m_utils;

		protected Base()
		{
			m_xsi = new CXSIApplicationClass();
			m_fact = new CXSIFactoryClass();
			m_utils = new CXSIUtilsClass();
		}
		protected bool Log(String str)
		{
			m_xsi.LogMessage(str, siSeverity.siVerbose);
			return true;
		}

		protected bool Info(String str)
		{
			m_xsi.LogMessage(str, siSeverity.siInfo);
			return true;
		}

		protected bool Error(String str)
		{
			m_xsi.LogMessage(str, siSeverity.siError);
			return true;
		}

		protected XSIApplication GetXSI()
		{
			return m_xsi;
		}

		protected XSIFactory GetFactory()
		{
			return m_fact;
		}

		protected XSIUtils GetUtils()
		{
			return m_utils;
		}
	}
}