using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GAF;
using GAF.Extensions;
using GAF.Operators;
using System.Drawing;
using System.Windows.Forms;
using AForge.Fuzzy;
using System.Threading;

namespace load_aircraft
{
    public partial class Form1 : Form
    {
        //variables
        public static int numberOfBoxes = 6;
        public static int rectangleSize = 80;
        public static int holdLength = 600;
        public static int holdWidth = 300;
        public static List<Rectangle> rectangleList;
        public static List<int> weightList;
        public static System.Drawing.Graphics graphicsObj;
        public static Rectangle aircraftHold;
        public static TextBox textbox;
        public static bool zeroGeneration = true;
        private static Point fuzzyCenter;
        private static Population population;
        private static Population populationZero;
        private static bool fuzzyRun = true;
        private static List<Box> boxesZeroGeneration = new List<Box>();
        public static Rectangle leftMargin;
        public static Rectangle rightMargin;
        public static Rectangle topMargin;
        public static Rectangle bottomMargin;

        public Form1()
        {
            InitializeComponent();
            rectangleList = new List<Rectangle>();
            weightList = new List<int>();
            Width = holdWidth + 60;
            Height = holdLength + 100;
        }
        private void buttonGenerate_Click(object sender, EventArgs e)
        {
            population = new Population();
            List<Box> boxes = new List<Box>();
            //generate weights for the given number of boxes
            Random rnd = new Random();
            for (int i = 0; i < numberOfBoxes; i++)
            {
                weightList.Add(rnd.Next(0, 100));
            }
            //create the chromosomes
            for (var p = 0; p < 1000; p++)
            {
                //generates coordinates for the X number of boxes
                for (int x = 0; x < numberOfBoxes; x++)
                {
                    int horizontal = rnd.Next(aircraftHold.Left + 40, aircraftHold.Right - 40);
                    int vertical = rnd.Next(aircraftHold.Top + 40, aircraftHold.Bottom - 40);
                    boxes.Add(new Box() { Name = "Box" + (x + 1), CoordinateX = horizontal, CoordinateY = vertical, Weight = weightList[x] });
                }
                // the x number of boxes will represent a chromosome
                var chromosome = new Chromosome();
                foreach (var box in boxes)
                {
                    chromosome.Genes.Add(new Gene(box));
                }
                // add the chromosome to the population
                population.Solutions.Add(chromosome);

                // check if there is intersection in the chromosome*********************************************************************************
                bool noIntersection = generateRectangles(chromosome);
                //bool noIntersection = true;
                // *********************************************************************************************************************************

                // if there is no intersection, the boxes will be displayed
                if (noIntersection == true)
                {
                    // display the very first generation
                    if (zeroGeneration == true)
                    {
                        // add the chromosome to the populationZero so it can be used in Fuzzy Logic
                        populationZero = new Population();
                        populationZero.Solutions.Add(chromosome);
                        //draw the boxes one by one on the form
                        for (int i = 0; i < boxes.Count; i++)
                        {
                            Pen myPen = new Pen(System.Drawing.Color.DarkGray, 1);
                            Rectangle rect = new Rectangle(boxes[i].CoordinateX - (rectangleSize / 2), boxes[i].CoordinateY - (rectangleSize / 2), rectangleSize, rectangleSize);
                            graphicsObj.DrawRectangle(myPen, rect);

                            System.Drawing.Font drawFont = new System.Drawing.Font("Arial", 16);
                            System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.DarkGray);
                            System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat();
                            graphicsObj.DrawString(boxes[i].Name + "\n" + boxes[i].Weight + "kg", drawFont, drawBrush, boxes[i].CoordinateX - 30, boxes[i].CoordinateY - 26, drawFormat);
                            Point point = new Point (boxes[i].CoordinateX, boxes[i].CoordinateY);
                        }
                        // calculate the distance, so it can be compared to the fittest generation
                        Point com = centerOfMassCalculator(chromosome);
                        Pen myPenCross = new Pen(System.Drawing.Color.Gray, 2);
                        graphicsObj.DrawLine(myPenCross, com.X - 5, com.Y - 5, com.X + 5, com.Y + 5);
                        graphicsObj.DrawLine(myPenCross, com.X + 5, com.Y - 5, com.X - 5, com.Y + 5);
                        Console.WriteLine("Generation: {0}, Fitness: {1}, Distance: {2}", "0", CalculateFitness(chromosome), CalculateDistance(chromosome));
                    }
                    zeroGeneration = false;
                }
                boxes.Clear();
            }
        }

        private void buttonGenetic_Click(object sender, EventArgs e)
        {
            // check whether data was generated or not 
            if (population != null)
            {
                SetupGeneticAlgorithm();
            }
            else
            {
                // if no, show it in a message box
                MessageBox.Show("You must generate data");
            }
        }

        private void buttonFuzzy_Click(object sender, EventArgs e)
        {
            // check whether data was generated or not 
            if (population != null)
            {
                SetupFuzzy();
            }
            else
            {
                // if no, show it in a message box
                MessageBox.Show("You must generate data");
            }
        }

        private static void SetupFuzzy()
        {
            // creating 3 fuzzy sets to represent forward, ideal, rearward
            //aircraftHold.Top and aircraftHold.Left values are important, since there are 20, 50 px margins
            FuzzySet fsForward = new FuzzySet("Forward", new TrapezoidalFunction(aircraftHold.Top + 250, aircraftHold.Top + 300, TrapezoidalFunction.EdgeType.Right));
            FuzzySet fsIdealY = new FuzzySet("Ideal", new TrapezoidalFunction(aircraftHold.Top + 299, aircraftHold.Top + 300, aircraftHold.Top + 300, aircraftHold.Top + 301));
            FuzzySet fsRearward = new FuzzySet("Rearward", new TrapezoidalFunction(aircraftHold.Top + 300, aircraftHold.Top + 350, TrapezoidalFunction.EdgeType.Left));

            // creating linguistic variables for the input
            LinguisticVariable lvAxisY = new LinguisticVariable("yDistance", aircraftHold.Top, aircraftHold.Top + 600);
            lvAxisY.AddLabel(fsForward);
            lvAxisY.AddLabel(fsIdealY);
            lvAxisY.AddLabel(fsRearward);

            // creating 3 fuzzy sets to represent left, ideal, right
            FuzzySet fsLeft = new FuzzySet("Left", new TrapezoidalFunction(aircraftHold.Left + 100, aircraftHold.Left + 150, TrapezoidalFunction.EdgeType.Right));
            FuzzySet fsIdealX = new FuzzySet("Ideal", new TrapezoidalFunction(aircraftHold.Left + 149, aircraftHold.Left + 150, aircraftHold.Left + 150, aircraftHold.Left + 151));
            FuzzySet fsRight = new FuzzySet("Right", new TrapezoidalFunction(aircraftHold.Left + 150, aircraftHold.Left + 200, TrapezoidalFunction.EdgeType.Left));

            // creating linguistic variables for the input
            LinguisticVariable lvAxisX = new LinguisticVariable("xDistance", aircraftHold.Left, aircraftHold.Left + 300);
            lvAxisX.AddLabel(fsLeft);
            lvAxisX.AddLabel(fsIdealX);
            lvAxisX.AddLabel(fsRight);

            // linguistic labels (fuzzy sets) that compose the direction
            FuzzySet fsDirZero = new FuzzySet("fsDirZero", new TrapezoidalFunction(0, 0, 0, 0));
            FuzzySet fsDirForward = new FuzzySet("fsDirForward", new TrapezoidalFunction(-45, 0, 0, 45));
            FuzzySet fsDirLBackward = new FuzzySet("fsDirLBackward", new TrapezoidalFunction(-180, -135, TrapezoidalFunction.EdgeType.Right));
            FuzzySet fsDirRBackward = new FuzzySet("fsDirRBackward", new TrapezoidalFunction(135, 180, TrapezoidalFunction.EdgeType.Left));
            FuzzySet fsDirLeft = new FuzzySet("fsDirLeft", new TrapezoidalFunction(-135, -90, -90, -45));
            FuzzySet fsDirRight = new FuzzySet("fsDirRight", new TrapezoidalFunction(45, 90, 90, 135));
            FuzzySet fsDirForwardLeft = new FuzzySet("fsDirForwardLeft", new TrapezoidalFunction(-90, -45, -45, 0));
            FuzzySet fsDirForwardRight = new FuzzySet("fsDirForwardRight", new TrapezoidalFunction(0, 45, 45, 90));
            FuzzySet fsDirBackwardLeft = new FuzzySet("fsDirBackwardLeft", new TrapezoidalFunction(-180, -135, -135, -90));
            FuzzySet fsDirBackwardRight = new FuzzySet("fsDirBackwardRight", new TrapezoidalFunction(90, 135, 135, 180));

            ////show membership to the Cool set for some values
            //for (int j = -180; j <= 180; j++)
            //    {
            //        Console.WriteLine(j + " B " + fsDirLBackward.GetMembership(j) + " BL " + fsDirBackwardLeft.GetMembership(j) + " L " + fsDirLeft.GetMembership(j) + " FL " + fsDirForwardLeft.GetMembership(j) + " F " + fsDirForward.GetMembership(j) + " FR " + fsDirForwardRight.GetMembership(j) + " R " + fsDirRight.GetMembership(j) + " BR " + fsDirBackwardRight.GetMembership(j) + " B " + fsDirRBackward.GetMembership(j));
            //    }

            // creating linguistic variables for the output
            LinguisticVariable lvDir = new LinguisticVariable("Direction", -180, 180);
            lvDir.AddLabel(fsDirZero);
            lvDir.AddLabel(fsDirForward);
            lvDir.AddLabel(fsDirLBackward);
            lvDir.AddLabel(fsDirRBackward);
            lvDir.AddLabel(fsDirLeft);
            lvDir.AddLabel(fsDirRight);
            lvDir.AddLabel(fsDirForwardLeft);
            lvDir.AddLabel(fsDirForwardRight);
            lvDir.AddLabel(fsDirBackwardLeft);
            lvDir.AddLabel(fsDirBackwardRight);

            // the database
            Database fuzzyDB = new Database();
            fuzzyDB.AddVariable(lvAxisY);
            fuzzyDB.AddVariable(lvAxisX);
            fuzzyDB.AddVariable(lvDir);

            // creating the inference system
            InferenceSystem IS = new InferenceSystem(fuzzyDB, new CentroidDefuzzifier(1000));

            // declare the rules
            IS.NewRule("Rule 1", "IF yDistance IS Forward AND xDistance IS Ideal THEN Direction IS fsDirRBackward");
            IS.NewRule("Rule 2", "IF yDistance IS Forward AND xDistance IS Left THEN Direction IS fsDirBackwardRight");
            IS.NewRule("Rule 3", "IF yDistance IS Forward AND xDistance IS Right THEN Direction IS fsDirBackwardLeft");
            IS.NewRule("Rule 4", "IF yDistance IS Rearward AND xDistance IS Ideal THEN Direction IS fsDirForward");
            IS.NewRule("Rule 5", "IF yDistance IS Rearward AND xDistance IS Left THEN Direction IS fsDirForwardRight");
            IS.NewRule("Rule 6", "IF yDistance IS Rearward AND xDistance IS Right THEN Direction IS fsDirForwardLeft");
            IS.NewRule("Rule 7", "IF yDistance IS Ideal AND xDistance IS Ideal THEN Direction IS fsDirZero");
            IS.NewRule("Rule 8", "IF yDistance IS Ideal AND xDistance IS Left THEN Direction IS fsDirRight");
            IS.NewRule("Rule 9", "IF yDistance IS Ideal AND xDistance IS Right THEN Direction IS fsDirLeft");
            IS.NewRule("Rule 10", "IF yDistance IS Forward AND xDistance IS Ideal THEN Direction IS fsDirLBackward");

            // loop through the population so the boxes can be added to a list for collision detection
            foreach (var gene in populationZero.GetTop(1)[0].Genes)
            {
                Box box = (Box)gene.ObjectValue;
                int centerX = box.CoordinateX - (rectangleSize / 2);
                int centerY = box.CoordinateY - (rectangleSize / 2);
                Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                rectangleList.Add(generatedRectangle);
            }
            int steps = 0;

            // a while method runs until the distance becomes 0
            while (fuzzyRun)
            {
                // calculate the center of mass of the boxes
                fuzzyCenter = centerOfMassCalculator(populationZero.GetTop(1)[0]);

                Console.WriteLine("Fuzzy Logic, Center Of Mass: x:" + fuzzyCenter.X + " y:" + fuzzyCenter.Y + ", Distance: " + CalculateDistance(populationZero.GetTop(1)[0]) + ", Steps: " + steps);
                //int milliseconds = 125;
                //Thread.Sleep(milliseconds);
                               
                // setting inputs
                IS.SetInput("xDistance", fuzzyCenter.X);
                IS.SetInput("yDistance", fuzzyCenter.Y);

                //double angle = IS.Evaluate("Direction");
                //Console.WriteLine("B " + fsDirLBackward.GetMembership((int)angle) + " BL " + fsDirBackwardLeft.GetMembership((int)angle) + " L " + fsDirLeft.GetMembership((int)angle) + " FL " + fsDirForwardLeft.GetMembership((int)angle) + " F " + fsDirForward.GetMembership((int)angle) + " FR " + fsDirForwardRight.GetMembership((int)angle) + " R " + fsDirRight.GetMembership((int)angle) + " BR " + fsDirBackwardRight.GetMembership((int)angle) + " B " + fsDirRBackward.GetMembership((int)angle));

                string command = "";
                float cmd = 0;

                // calculate fuzzy output
                FuzzyOutput fuzzyOutput = IS.ExecuteInference("Direction");
                foreach (FuzzyOutput.OutputConstraint oc in fuzzyOutput.OutputList)
                {
                    // the value with the bigegst membership degree will be the command
                    if (oc.FiringStrength > cmd)
                    {
                        cmd = oc.FiringStrength;
                        command = oc.Label;
                    }
                }
                // starts step counter, call startMovemment to perform action 
                steps++;
                startMovement(command, populationZero.GetTop(1)[0]);
            }

            // when the final solution is calculated the boxes are drawn on the form
            int i = 0;
            var fittest = populationZero.GetTop(1)[0];
            foreach (var gene in fittest.Genes)
            {
                i++;
                Pen myPen = new Pen(System.Drawing.Color.Blue, 1);
                Rectangle box = new Rectangle(((Box)gene.ObjectValue).CoordinateX - (rectangleSize / 2), ((Box)gene.ObjectValue).CoordinateY - (rectangleSize / 2), rectangleSize, rectangleSize);
                graphicsObj.DrawRectangle(myPen, box);

                System.Drawing.Font drawFont = new System.Drawing.Font("Arial", 16);
                System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Blue);
                System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat();
                graphicsObj.DrawString("Box"+i + "\n" + ((Box)gene.ObjectValue).Weight + "kg", drawFont, drawBrush, ((Box)gene.ObjectValue).CoordinateX - 30, ((Box)gene.ObjectValue).CoordinateY - 26, drawFormat);
            }
            // the center of mass is will be drawn as an X 
            Pen myPenCross = new Pen(System.Drawing.Color.Blue, 2);
            graphicsObj.DrawLine(myPenCross, fuzzyCenter.X - 5, fuzzyCenter.Y - 5, fuzzyCenter.X + 5, fuzzyCenter.Y + 5);
            graphicsObj.DrawLine(myPenCross, fuzzyCenter.X + 5, fuzzyCenter.Y - 5, fuzzyCenter.X - 5, fuzzyCenter.Y + 5);
        }

        private static void SetupGeneticAlgorithm()
        {
            //create the elite operator
            var elite = new Elite(5);

            //create the crossover operator
            var crossover = new Crossover(0.8)
            {
                CrossoverType = CrossoverType.SinglePoint //SinglePoint or DoublePoint
            };

            //create the mutation operator
            var mutate = new SwapMutate(0.02);

            //create the GA
            var ga = new GeneticAlgorithm(population, CalculateFitness);

            //hook up to some useful events
            ga.OnGenerationComplete += ga_OnGenerationComplete;
            ga.OnRunComplete += ga_OnRunComplete;

            //add the operators
            ga.Operators.Add(elite);
            ga.Operators.Add(crossover);
            ga.Operators.Add(mutate);

            //run the GA
            ga.Run(Terminate);
        }
  
        static void ga_OnRunComplete(object sender, GaEventArgs e)
        {
            //when the algorithm finishes the best solution is drawn on the form
            var fittest = e.Population.GetTop(1)[0];
            foreach (var gene in fittest.Genes)
            {
                Pen myPen = new Pen(System.Drawing.Color.Red, 1);
                Rectangle box = new Rectangle(((Box)gene.ObjectValue).CoordinateX - (rectangleSize / 2), ((Box)gene.ObjectValue).CoordinateY - (rectangleSize / 2), rectangleSize, rectangleSize);
                graphicsObj.DrawRectangle(myPen, box);

                System.Drawing.Font drawFont = new System.Drawing.Font("Arial", 16);
                System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);
                System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat();
                graphicsObj.DrawString(((Box)gene.ObjectValue).Name + "\n" + ((Box)gene.ObjectValue).Weight + "kg", drawFont, drawBrush, ((Box)gene.ObjectValue).CoordinateX - 30, ((Box)gene.ObjectValue).CoordinateY - 26, drawFormat);
            }
            // teh center of mass is an X
            Point centerOfMass = centerOfMassCalculator(fittest);
            Pen myPenCross = new Pen(System.Drawing.Color.Red, 2);
            graphicsObj.DrawLine(myPenCross, centerOfMass.X - 5, centerOfMass.Y - 5, centerOfMass.X + 5, centerOfMass.Y + 5);
            graphicsObj.DrawLine(myPenCross, centerOfMass.X + 5, centerOfMass.Y - 5, centerOfMass.X - 5, centerOfMass.Y + 5);
        }

        private static void ga_OnGenerationComplete(object sender, GaEventArgs e)
        {
            // when a there is a new generation, its fittness is calculated (distance from center of the aircraft)
            var fittest = e.Population.GetTop(1)[0];
            var distanceFromCenter = CalculateDistance(fittest);
            var center = centerOfMassCalculator(fittest);
            // dispalyed iin console line
            Console.WriteLine("Genetic Algorithm, Center Of Mass: x:" + center.X + " y:" + center.Y + ", Distance: " + distanceFromCenter + ", Generation: " + e.Generation);
        }

        public static bool Terminate(Population population,
            int currentGeneration, long currentEvaluation)
        {
            //return currentEvaluation > 0.999988;
            return currentGeneration > 99;
        }
        public static double CalculateFitness(Chromosome chromosome)
        {
            // call the method which check if tehre is intersection or not
            bool noIntersection = generateRectangles(chromosome);
            // if there is no intersection then the fittness value can be calculated
            if (noIntersection == true)
            {
                var distanceFromCenter = CalculateDistance(chromosome);
                return 1 - distanceFromCenter / 1000000;
            }
            // if there any of the boxes intersects with another one, teh fitness is 0
            else
            {
                return 0;
            }
        }

        private static double CalculateDistance(Chromosome chromosome)
        {
            // calculate the COM distance from the center of the aircraft
            var centerOfMass = centerOfMassCalculator(chromosome);
            double xDelta = aircraftHold.Left + aircraftHold.Width / 2 - centerOfMass.X;
            double yDelta = aircraftHold.Top + aircraftHold.Height / 2 - centerOfMass.Y;
            var distanceFromCenter = System.Math.Sqrt(System.Math.Pow(xDelta, 2) + System.Math.Pow(yDelta, 2));

            return distanceFromCenter;
        }

        private static bool generateRectangles(Chromosome chromosome)
        {
            // generate a new rectangle and checks if it intersects with any of the boxes that already exist
            bool canBePlaced = true;
            Rectangle generatedRectangle = new Rectangle(0, 0, 0, 0);
            foreach (var gene in chromosome.Genes)
            {
                Box box = (Box)gene.ObjectValue;
                int centerX = box.CoordinateX - (rectangleSize / 2);
                int centerY = box.CoordinateY - (rectangleSize / 2);
                generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                // if there is collision then the it returns false
                bool value = checkIntersection(generatedRectangle);
                if (value == true)
                {
                    canBePlaced = false;
                }
                // add the rectangle to the list
                rectangleList.Add(generatedRectangle);
            }
            rectangleList.Clear();
            return canBePlaced;
        }

        private static bool checkIntersection(Rectangle rect)
        {
            bool intersect = true;
            int num = 0;
            // if it is the first run (number of rectangles is 0), then there cannot be intersection
            if (rectangleList.Count == 0)
            {
                intersect = false;
            }
            else
            {
                // test against the existing rectangles
                for (int x = 0; x < rectangleList.Count; x++)
                {
                    if (!rect.IntersectsWith(rectangleList[x]))
                    {
                        // if they don't intersect then the number of the variable increases
                        num++;
                    }
                }
                // at the end it check the number of good rectangles are the same as the total number of boxes
                if (num == rectangleList.Count)
                {
                    intersect = false;
                }
            }
            return intersect;
        }

        private static bool moveFuzzyRectangles(Point point)
        {
            // check for collisions with other boxes or the wall
            bool canBePlaced = true;
            Rectangle generatedRectangle = new Rectangle(0, 0, 0, 0);
            int centerX = point.X - (rectangleSize / 2);
            int centerY = point.Y - (rectangleSize / 2);
            generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
            // other boxes
            bool valueRect = checkIntersection(generatedRectangle);
            // borders
            bool valueBord = checkBorderIntersection(generatedRectangle);
            if (valueRect || valueBord == true)
            {
                canBePlaced = false;
            }
            return canBePlaced;
        }

        private static bool checkBorderIntersection(Rectangle rect)
        {
            // detects if the box hits the wall
            bool intersect = true;
            if (!rect.IntersectsWith(leftMargin) && !rect.IntersectsWith(rightMargin) && !rect.IntersectsWith(topMargin) && !rect.IntersectsWith(bottomMargin))
            {
                intersect = false;
            }
            return intersect;
        }

        private static Point centerOfMassCalculator(Chromosome chromosome)
        {
            // calculate the center of mass
            int Xupper = 0;
            int Xlower = 0;
            int Yupper = 0;
            int Ylower = 0;

            //run through each box in the chromosome, sum the X values then divide by the sum of weights, same with Y values
            foreach (var gene in chromosome.Genes)
            {
                Box box = (Box)gene.ObjectValue;
                Xupper += box.CoordinateX * box.Weight;
                Xlower += box.Weight;
                Yupper += box.CoordinateY * box.Weight;
                Ylower += box.Weight;
            }
            // calucalte the center of mass and returns it as a Point object
            int comX = Xupper / Xlower;
            int comY = Yupper / Ylower;
            Point point = new Point(x: comX, y: comY);
            return point;
        }

        private static void startMovement(string command, Chromosome chromosome)
        {
            // cerate variables necessary for the method
            int i = 0;
            List<Box> boxes = new List<Box>();

            var newChromosome = new Chromosome();
            // switch - case statement using the command from fuzzy system
            switch (command)
            {
                // if the command is forward
                case "fsDirForward":
                    //run through each box in the in the chromosome
                    foreach (var gene in chromosome.Genes)
                    {
                        rectangleList[i] = new Rectangle(0, 0, 0, 0);
                        Box box = (Box)gene.ObjectValue;
                        // checks if the box can be moved to the given direction
                        Point point = new Point(box.CoordinateX, box.CoordinateY - 1);
                        // if it can be moved, then changes the x or y coordinate of the box in the chromosome
                        bool value = moveFuzzyRectangles(point);
                        if (value == true)
                        {
                            // changes the coordinate
                            box.CoordinateY = box.CoordinateY - 1;
                            // add as new gene
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            // add/update the location of the box
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        else
                        {
                            // if the box cannot be moved, then everything remains the same
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            // add/update the location of the box
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        i++;
                    }
                    // delete the last and only chromosome
                    populationZero.DeleteLast();
                    // add the new boxes as genes
                    foreach (var box in boxes)
                    {
                        newChromosome.Genes.Add(new Gene(box));
                    }
                    // add the chromosome to the population
                    populationZero.Solutions.Add(newChromosome);
                    break;
                // if the command is backward
                case "fsDirRBackward":
                    foreach (var gene in chromosome.Genes)
                    {
                        rectangleList[i] = new Rectangle(0, 0, 0, 0);
                        Box box = (Box)gene.ObjectValue;
                        Point point = new Point(box.CoordinateX, box.CoordinateY + 1);
                        bool value = moveFuzzyRectangles(point);
                        if (value == true)
                        {
                            box.CoordinateY = box.CoordinateY + 1;
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        else
                        {
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        i++;
                    }
                    // remove and add new chromosome
                    populationZero.DeleteLast();
                    foreach (var box in boxes)
                    {
                        newChromosome.Genes.Add(new Gene(box));
                    }
                    populationZero.Solutions.Add(newChromosome);

                    break;
                // if the command is left
                case "fsDirLeft":
                    foreach (var gene in chromosome.Genes)
                    {
                        rectangleList[i] = new Rectangle(0, 0, 0, 0);
                        Box box = (Box)gene.ObjectValue;
                        Point point = new Point(box.CoordinateX - 1, box.CoordinateY);
                        bool value = moveFuzzyRectangles(point);
                        if (value == true)
                        {
                            box.CoordinateX = box.CoordinateX - 1;
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        else
                        {
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        i++;
                    }
                    // remove and add new chromosome
                    populationZero.DeleteLast();
                    foreach (var box in boxes)
                    {
                        newChromosome.Genes.Add(new Gene(box));
                    }
                    populationZero.Solutions.Add(newChromosome);
                    break;
                // if the command is right
                case "fsDirRight":
                    foreach (var gene in chromosome.Genes)
                    {
                        rectangleList[i] = new Rectangle(0, 0, 0, 0);
                        Box box = (Box)gene.ObjectValue;
                        Point point = new Point(box.CoordinateX + 1, box.CoordinateY);
                        bool value = moveFuzzyRectangles(point);
                        if (value == true)
                        {
                            box.CoordinateX = box.CoordinateX + 1;
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        else
                        {
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        i++;
                    }
                    // remove and add new chromosome
                    populationZero.DeleteLast();
                    foreach (var box in boxes)
                    {
                        newChromosome.Genes.Add(new Gene(box));
                    }
                    populationZero.Solutions.Add(newChromosome);
                    break;
                // if the command is left-forward
                case "fsDirForwardLeft":
                    foreach (var gene in chromosome.Genes)
                    {
                        rectangleList[i] = new Rectangle(0, 0, 0, 0);
                        Box box = (Box)gene.ObjectValue;
                        Point point = new Point(box.CoordinateX - 1, box.CoordinateY - 1);
                        bool value = moveFuzzyRectangles(point);
                        if (value == true)
                        {
                            box.CoordinateX = box.CoordinateX - 1;
                            box.CoordinateY = box.CoordinateY - 1;
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        else
                        {
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        i++;
                    }
                    // remove and add new chromosome
                    populationZero.DeleteLast();
                    foreach (var box in boxes)
                    {
                        newChromosome.Genes.Add(new Gene(box));
                    }
                    populationZero.Solutions.Add(newChromosome);
                    break;
                // if the command is right-forward
                case "fsDirForwardRight":
                    foreach (var gene in chromosome.Genes)
                    {
                        rectangleList[i] = new Rectangle(0, 0, 0, 0);
                        Box box = (Box)gene.ObjectValue;
                        Point point = new Point(box.CoordinateX + 1, box.CoordinateY - 1);
                        bool value = moveFuzzyRectangles(point);
                        if (value == true)
                        {
                            box.CoordinateX = box.CoordinateX + 1;
                            box.CoordinateY = box.CoordinateY - 1;
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        else
                        {
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        i++;
                    }
                    // remove and add new chromosome
                    populationZero.DeleteLast();
                    foreach (var box in boxes)
                    {
                        newChromosome.Genes.Add(new Gene(box));
                    }
                    populationZero.Solutions.Add(newChromosome);
                    break;
                // if the command is left-backward
                case "fsDirBackwardLeft":
                    foreach (var gene in chromosome.Genes)
                    {
                        rectangleList[i] = new Rectangle(0, 0, 0, 0);
                        Box box = (Box)gene.ObjectValue;
                        Point point = new Point(box.CoordinateX - 1, box.CoordinateY + 1);
                        bool value = moveFuzzyRectangles(point);
                        if (value == true)
                        {
                            box.CoordinateX = box.CoordinateX - 1;
                            box.CoordinateY = box.CoordinateY + 1;
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        else
                        {
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        i++;
                    }
                    // remove and add new chromosome
                    populationZero.DeleteLast();
                    foreach (var box in boxes)
                    {
                        newChromosome.Genes.Add(new Gene(box));
                    }
                    populationZero.Solutions.Add(newChromosome);
                    break;
                // if the command is right-forward
                case "fsDirBackwardRight":
                    foreach (var gene in chromosome.Genes)
                    {
                        rectangleList[i] = new Rectangle(0, 0, 0, 0);
                        Box box = (Box)gene.ObjectValue;
                        Point point = new Point(box.CoordinateX + 1, box.CoordinateY + 1);
                        bool value = moveFuzzyRectangles(point);
                        if (value == true)
                        {
                            box.CoordinateX = box.CoordinateX + 1;
                            box.CoordinateY = box.CoordinateY + 1;
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        else
                        {
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        i++;
                    }
                    // remove and add new chromosome
                    populationZero.DeleteLast();
                    foreach (var box in boxes)
                    {
                        newChromosome.Genes.Add(new Gene(box));
                    }
                    populationZero.Solutions.Add(newChromosome);
                    break;
                // if the command is stop
                case "fsDirZero":
                    fuzzyRun = false;
                    break;
                // if the command is backward
                case "fsDirLBackward":
                    foreach (var gene in chromosome.Genes)
                    {
                        rectangleList[i] = new Rectangle(0, 0, 0, 0);
                        Box box = (Box)gene.ObjectValue;
                        Point point = new Point(box.CoordinateX, box.CoordinateY + 1);
                        bool value = moveFuzzyRectangles(point);
                        if (value == true)
                        {
                            box.CoordinateY = box.CoordinateY + 1;
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        else
                        {
                            boxes.Add(new Box() { Name = "Box" + (i + 1), CoordinateX = box.CoordinateX, CoordinateY = box.CoordinateY, Weight = box.Weight });
                            int centerX = box.CoordinateX - (rectangleSize / 2);
                            int centerY = box.CoordinateY - (rectangleSize / 2);
                            Rectangle generatedRectangle = new Rectangle(centerX, centerY, rectangleSize, rectangleSize);
                            rectangleList[i] = generatedRectangle;
                        }
                        i++;
                    }
                    // remove and add new chromosome with the new values
                    populationZero.DeleteLast();
                    foreach (var box in boxes)
                    {
                        newChromosome.Genes.Add(new Gene(box));
                    }
                    // add the chromosome to the population
                    populationZero.Solutions.Add(newChromosome);
                    break;
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            // the background, the aircraft hold and the x, y axis get drawn
            Pen myPen = new Pen(System.Drawing.Color.Black, 1);
            aircraftHold = new Rectangle(20, 50, holdWidth, holdLength);
            graphicsObj = this.CreateGraphics();
            graphicsObj.DrawRectangle(myPen, aircraftHold);
            Pen myPenDash = new Pen(System.Drawing.Color.Black, 1);
            myPenDash.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            graphicsObj.DrawLine(myPenDash, aircraftHold.Left + aircraftHold.Width / 2, aircraftHold.Top - 10, aircraftHold.Left + aircraftHold.Width / 2, aircraftHold.Bottom + 10);
            graphicsObj.DrawLine(myPenDash, aircraftHold.Left - 10, aircraftHold.Top + aircraftHold.Height / 2, aircraftHold.Right + 10, aircraftHold.Top + aircraftHold.Height / 2);

            leftMargin = new Rectangle(0, aircraftHold.Top, aircraftHold.Left, aircraftHold.Height);
            rightMargin = new Rectangle(aircraftHold.Right, aircraftHold.Top, aircraftHold.Left, aircraftHold.Height);
            topMargin = new Rectangle(aircraftHold.Left, 0, aircraftHold.Width, aircraftHold.Top);
            bottomMargin = new Rectangle(aircraftHold.Left, aircraftHold.Bottom, aircraftHold.Width, aircraftHold.Top);
        }
    }
}

namespace load_aircraft
{
    [Serializable]
    public class Box
    {
        public string Name { set; get; }
        public int CoordinateX { get; set; }
        public int CoordinateY { get; set; }
        public int Weight { get; set; }
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ CoordinateX.GetHashCode();
                hashCode = (hashCode * 397) ^ CoordinateY.GetHashCode();
                return hashCode;
            }
        }
    }
}