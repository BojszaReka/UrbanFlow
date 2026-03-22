using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Urbanflow.src.backend.models.enums;

namespace Urbanflow.src.backend.models.util
{
	public class VeszpremDistrict
	{
		public readonly static List<(string, List<string>)> DistrictNames = [
			("Bakonyalja", ["SA814", "SP1704", "SP1705", "SP1747", "SP1746", "SA815", "SA887", "SP1709", "SP1708", "SA831", "SP1706", "SP1707", "SA871", "SP1701", "SP1703", "SP1703", "SP1710", "SA870", "SP1711", "SP1856", "SP1632", "SA911", "SP1860"]),
			("Belváros",["SA853", "SP1661", "SP1660", "SP1895", "SP1824", "SA855", "SA868", "SP1645", "SP1629", "SA875", "SP1664", "SP1630", "SA901", "SP1670", "SA888", "SP1696", "SP1698", "SP1697", "SP1730", "SP1731", "SA894", "SP1683", "SA816", "SP1694", "SP1826", "SP1858", "SP1693"]),
			("Csatárhegy",["SP1891", "SA946", "SA933", "SP1867", "SP1890", "SP1900", "SA953", "SP1899", "SA827", "SP1677", "SA844", "SP1868", "SP1637"]),
			("Csolnoky város",["SP1811", "SA921", "SP1799", "SP1801", "SP1810", "SP1861", "SP1679", "SA825", "SP1798", "SA826", "SP1797", "SP1792", "SA829", "SP1791", "SP1795", "SA852", "SP1796", "SP1793", "SP1794", "SA872", "SA916", "SP1825", "SP1789", "SP1790", "SA917", "SP1625", "SP1649"]),
			("Dózsa város", ["SA817", "SP1723", "SP1724", "SA833", "SP1726", "SP1898", "SP1725", "SP1727", "SP1784", "SA858", "SP1719", "SA841", "SP1720", "SP1721", "SP1722", "SA903", "SP1893", "SA947", "SP1892", "SP1771", "SA884", "SP1772", "SP1770", "SP1769", "SA907", "SA908", "SP1676", "SA912", "SP1655", "SP1639"]),
			("Egry J. utcai lakótelep",["SP1809", "SP1808", "SA834", "SA835", "SP1818", "SP1819", "SP1820", "SA821", "SP1821", "SA879", "SP1817", "SP1816", "SA898", "SP1823", "SP1822"]),
			("Egyetemi város",["SA854", "SP1757", "SP1758", "SA836", "SP1761", "SP1762", "SA866", "SP1668", "SP1621", "SA896", "SP1765", "SP1766", "SP1763", "SA897", "SP1764"]),
			("Endrődi Sándor lakótelep / Jeruzsálemhegy",["SP1751", "SA837", "SP1752", "SA838", "SP1736", "SA848", "SP1737", "SP1753", "SA909", "SA886", "SP1754"]),
			("Füredi domb",["SP1827", "SP1623", "SA813", "SP1872", "SP1669", "SA842", "SP1814", "SP1815", "SP1646", "SA949", "SP1622", "SA948", "SA876", "SP1813", "SP1812", "SP1631", "SA902", "SP1671"]),
			("Gyulafirátót",["SP1896", "SP1853", "SA929", "SP1837", "SP1662", "SA845", "SA951", "SA846", "SP1732", "SP1733", "SP1749", "SP1748", "SA847"]),
			("Iparváros",["SA812", "SP1617", "SP1633", "SP1750", "SA818", "SP1864", "SA950", "SP1865", "SP1842", "SA936", "SP1841", "SA828", "SP1635", "SA948", "SA851", "SP1628", "SA856", "SP1643", "SP1618", "SP1634", "SA867", "SP1672", "SP1717", "SA890", "SP1718", "SP1666", "SA932", "SP1880", "SP1650", "SP1626", "SA882", "SA883", "SP1665", "SP1883", "SP1884", "SA940", "SA819", "SP1874", "SP1874", "SP1866", "SA938"]),
			("Jutaspuszta", ["SP1714", "SA861", "SP1715", "SP1716", "SA862", "SP1713", "SP1712", "SA865"]),
			("Jutasi úti lakótelep / Haszkovó lakótelep",["SP1881", "SA849", "SP1673", "SP1742", "SP1743", "SA850", "SA859", "SP1700", "SP1699", "SP1619", "SA860", "SP1644", "SP1756", "SA877", "SP1755", "SA874", "SP1805", "SP1804"]),
			("Kádárta",["SP1638", "SP1647", "SA904", "SP1640", "SA824", "SP1641", "SA924", "SP1846", "SP1847", "SP1850", "SA926", "SP1851", "SA925", "SP1848", "SP1849", "SP1833", "SA927", "SP1834", "SP1835", "SA928", "SP1852", "SP1882", "SA864", "SP1675", "SP1885", "SA941"]),
			("Pajtakert",["SA830", "SP1778", "SP1776", "SA880", "SP1777", "SA881"] ),
			("Szabadságpuszta", ["SP1744", "SP1887", "SA895", "SA920", "SP1838", "SA869", "SP1658", "SP1745", "SA873", "SP1657"]),
			("Takácskert",["SA857", "SP1667", "SP1620", "SA899", "SP1768", "SP1767", "SP1897", "SP1863", "SP1674", "SP1857", "SA910"]),
			("Újtelep",["SA939", "SP1879", "SA822", "SP1785", "SP1786", "SP1788", "SP1787", "SA823", "SP1802", "SA839", "SP1803", "SP1738", "SP1739", "SA889", "SP1807", "SA906", "SP1806", "SP1740", "SP1741", "SA878"]),
			("Újtelep 2",["SA811", "SP1681", "SP1682", "SP1773", "SA832", "SA863", "SP1800", "SP1760", "SA891", "SP1759", "SP1875", "SA944"]),
			("Veszprém völgy",["SA952", "SP1782", "SP1775", "SA885", "SP1780", "SP1779", "SP1774", "SA892", "SP1728", "SA905", "SP1729", "SA918", "SP1781", "SA919", "SP1734", "SP1735"])	
			];

		public readonly static List<(ENodeType, List<string>)> specificStops = [
			(ENodeType.Terminal, ["SP1856", "SP1632", "SA911", "SP1860", "SP1881", "SA849", "SP1673", "SA828", "SP1635", "SP1863", "SP1674", "SP1857", "SA910", "SP1866", "SA938", "SP1751", "SA837", "SP1893", "SA947", "SP1892", "SP1882", "SA864", "SP1675", "SP1633", "SP1750", "SA818", "SP1891", "SA946", "SA869", "SP1658", "SA883", "SP1665", "SP1662", "SA845", "SP1771", "SA884", "SP1772"]),
			(ENodeType.Junction, ["SA853", "SP1661", "SP1660", "SP1895", "SP1683", "SA816", "SP1694", "SP1826", "SP1858", "SP1693", "SP1861", "SP1679", "SA825", "SP1742", "SP1743", "SA850", "SP1864", "SA950", "SP1865", "SA866", "SP1668", "SP1621", "SP1728", "SA905", "SP1729", "SP1726",  "SP1898", "SP1725", "SP1727", "SP1730", "SP1731", "SA894", "SP1827", "SP1623", "SA813", "SP1872", "SP1669", "SA842", "SP1814", "SP1815"])
			
		];
	}
}
