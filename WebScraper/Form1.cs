/**
 * Author:    Ro Erb
 * Partner:   None
 * Date:      11/3/2020
 * Course:    CS 4540, University of Utah, School of Computing
 * Copyright: CS 4540 and Ro Erb - This work may not be copied for use in Academic Coursework.
 *
 * I, Ro Erb, certify that I wrote this code from scratch and did 
 * not copy it in part or whole from another source.  Any references used 
 * in the completion of the assignment are cited in my README file and in
 * the appropriate method header.
 *
 * File Contents
 *
 *    The Selenium_WebDriver is a Forms application that uses Selenium and
 *    ChromeDriver to scrape University of Utah's catalog and CS course
 *    web pages.
 */

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using OpenQA.Selenium.Support.UI;

namespace WebScraper
{
    public partial class Form1 : Form
    {
        private ChromeDriver _driver;

        private Dictionary<string, string> Courses { get; set; }

        /// <summary>
        /// The Form1 constructor initializes the form and creates the 
        /// courses dictionary used as the internal data structure for
        /// the course information.
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            Courses = new Dictionary<string, string>();
        }

        /// <summary>
        /// This method is called when the user has specified a year,
        /// semester, and optionally a result limit. The method then
        /// opens the necessary web pages, gathers the course info
        /// and stores it in the courses dictionary.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FindButton_Click(object sender, EventArgs e)
        {
            var semester = semesterComboBox.Text;
            var year = yearComboBox.Text.Substring(2);

            int urlSemester;
            if (semester.ToString() == "Fall")
            {
                urlSemester = 8;
            }
            else if (semester.ToString() == "Spring")
            {
                urlSemester = 4;
            }
            else
            {
                urlSemester = 6;
            }

            var url = "https://student.apps.utah.edu/uofu/stu/ClassSchedules/main/1" + year + urlSemester + "/class_list.html?subject=CS";

            _driver = new ChromeDriver();

            try
            {
                _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                _driver.Navigate().GoToUrl(url);
            }
            catch (Exception error)
            {
                _driver.Quit();
                MessageBox.Show("Web page not found: " + error.ToString());
            }

            var coursesExists = _driver.FindElementByTagName("h3").Text;
            if (coursesExists == "No classes found for subject.")
            {
                _driver.Quit();
                MessageBox.Show("No classes found for this semester and year.");
                return;
            }

            _driver.FindElementByLinkText("Seating availability for all CS classes").Click();
            _driver.Manage().Window.Maximize();

            List<string> courseTitles = new List<string>();

            var tableBody = _driver.FindElement(By.TagName("tbody"));
            var tableRows = tableBody.FindElements(By.TagName("tr"));

            string courseValue;
            foreach (var row in tableRows)
            {
                if (courseTitles.Count().ToString() == resultLimitTextBox.Text)
                {
                    break;
                }

                var td = row.FindElements(By.TagName("td"));

                if (td.ElementAt(3).Text == "001" && !td.ElementAt(2).Text.StartsWith("7") && td.ElementAt(2).Text.Length > 3)
                {
                    if (!td.ElementAt(4).Text.Contains("Seminar") && !td.ElementAt(4).Text.Contains("Special Topics"))
                    {
                        courseValue = td.ElementAt(1).Text + "," + td.ElementAt(2).Text + "," + td.ElementAt(4).Text + "," + td.ElementAt(7).Text + "," + semester + "," + year;
                        Courses.Add(td.ElementAt(1).Text + " " + td.ElementAt(2).Text, courseValue);
                        courseTitles.Add(td.ElementAt(1).Text + " " + td.ElementAt(2).Text);
                    }
                }
            }

            _driver.Navigate().Back();

            foreach (var course in courseTitles)
            {
                var current = _driver.FindElementByLinkText(course);
                var cardBody = current.FindElement(By.XPath("./parent::*/parent::*/parent::*"));
                var unit = cardBody.FindElement(By.XPath("div/ul/li[contains(text(), 'Units')]/span")).Text;
                string[] arr = Courses[course].Split(",");

                cardBody.FindElement(By.LinkText("Class Details")).Click();
                var header = _driver.FindElement(By.XPath("//div[contains(text(), 'Description')]//following-sibling::div"));
                var description = header.FindElement(By.ClassName("col")).Text;
                courseValue = arr[0] + "," + arr[1] + "," + unit + "," + arr[2] + "," + arr[3] + "," + arr[4] + "," + arr[5] + "," + '"' + description + '"';

                Courses[course] = courseValue;

                _driver.Navigate().Back();
            }

            saveButton.Show();
            _driver.Quit();
        }

        /// <summary>
        /// This method is called when the Save button is clicked 
        /// and calls the SaveToFile method to instigate the save
        /// process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveToFile();
        }

        /// <summary>
        /// This method is called when the Save button is clicked 
        /// and is responsible for setting all of the variables
        /// needed to save to a csv file.
        /// </summary>
        private void SaveToFile()
        {
            saveFileDialog1.InitialDirectory = @"C:\";
            saveFileDialog1.Title = "Save Course List";
            saveFileDialog1.CheckPathExists = true;
            saveFileDialog1.DefaultExt = "csv";
            saveFileDialog1.Filter = "Text files (*.csv)|*.csv|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;
            saveFileDialog1.FileName = "U_CS_Courses";

            saveFileDialog1.ShowDialog();
        }

        /// <summary>
        /// This method is called when the Save file dialog
        /// prompt appears. The Courses that are stored within 
        /// the Courses dictionary are saved in a csv file given 
        /// the user specified file location.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            var filename = saveFileDialog1.FileName;

            var fileContents = "Course Dept,Course Number,Course Credits,Course Title,Course Enrollment,Course Semester,Course Year,Course Description" + "\n";
            foreach (var course in Courses)
            {
                fileContents += course.Value + "\n";
            }

            File.WriteAllText(filename, fileContents);
        }

        /// <summary>
        /// This method is called when the Search button
        /// is clicked and creates a ChromeDriver object
        /// to find the course description of a given
        /// course.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchButton_Click(object sender, EventArgs e)
        {
            var url = "https://catalog.utah.edu";

            _driver = new ChromeDriver();
            _driver.Navigate().GoToUrl(url);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
            _driver.Manage().Window.Maximize();

            _driver.FindElementByLinkText("Courses").Click();

            try
            {
                _driver.FindElement(By.Id("Search")).SendKeys(searchCourseTextBox.Text);
                _driver.Navigate().GoToUrl("https://catalog.utah.edu/#/courses?searchTerm=" + searchCourseTextBox.Text);
                _driver.FindElementByLinkText(searchCourseTextBox.Text.ToUpper()).Click();
            }
            catch (Exception error)
            {
                _driver.Quit();
                MessageBox.Show("Did not find search result: " + error.ToString());
            }

            var description = _driver.FindElement(By.XPath("//h3[contains(text(), 'Course Description')]//following-sibling::div/div/div/div/div")).Text;
            richTextBox1.Text = description;

            _driver.Quit();
        }
    }
}