﻿using System;
using System.Collections.Generic;
using static System.Console;

struct Edge {
    public string Name { get; init; }
    // public string From { get; init; } I will put edges into questions, making From the name of the quesiton they are conatained in
    public string To { get; init; }
    public Edge(string n, string to ) { 
        Name = n; To = to; 
    }
    public Edge(string to) {
        Name = "NOT SET"; To = to;
    }
}

abstract class Question {               // Vertex with dialog data
    public string name;                 // id as vertex, these id's are used in Edges from and to
    public string question;             // actual question
    public List<Edge> options;        // for string options type: options. Note options must match edges IDs - names. 

    public Question(string n, string q, List<Edge> o) {
        name = n; question = q; options = o;
    }

    public Question() { } // in order to allow custom construcotrs in children

    public abstract bool IsAnswerValid(string answer, out string nextQuestion);

    public new abstract string GetType();
}

class OptionsQ : Question {
    public OptionsQ(string n, string q, List<Edge> o ) : base(n, q, o) { }
    // !! edges in optionsQ must have unique name, start checking that too later 
    public override bool IsAnswerValid(string answer, out string nextQuestion) {
        // answer must match one of the names of edges in options
        // when it does: 
        //      1. set nextQuestion to match optons.edge.To (of the edge with corresponding name)
        //      2. return true
        // otherwise return false
        foreach(Edge e in options)
            if(answer == e.Name) {
                nextQuestion = e.To;
                return true;
            }
        nextQuestion = "";
        return false;
    }

    public override string GetType() => "Options question";
}

class IntQ : Question {
    public int min, max;
    public IntQ (string n, string q, List<Edge> o, int min, int max) : base(n, q, o) {
        this.min = min; this.max = max;
    }

    // >>> !!! At this point IntQuestion assume to have one option. Code the ability to have more options later. !!!
    public override bool IsAnswerValid(string answer, out string nextQuestion) {
        int number;
        if (int.TryParse(answer, out number)) {
            nextQuestion = options[0].To;
            return (min <= number && number <= max); // number must inside the min max range
        }
        nextQuestion = "Input string is not parsable into int.";
        return false;
    }

    public override string GetType() => "Integer question";
}

class EmailQ : Question {
    public EmailQ(string n, string q, List<Edge> o) : base(n, q, o) { }

    // in future add parsing of email through apropriate reg exp 
    // which will decide true/false
    public override bool IsAnswerValid(string answer, out string nextQuestion) {
        nextQuestion = options[0].To;
        return true;
    }

    public override string GetType() => "Email question";
}

class BoundaryVertex : Question {
    public BoundaryVertex( string n ) { name = n; }

    public BoundaryVertex( string n, List<Edge> direction ) {
        if (direction.Count != 1)
            throw new Exception("Boundary Vertex of dialog with an out edge must have exactly one edge.");

        name = n;
    }

    public override bool IsAnswerValid(string answer, out string nextQuestion) {
        if( name == "START" ) {
            nextQuestion = options[0].To;
            return true;
        }
        nextQuestion = "END";
        return false;
    }
    public override string GetType() => "Boundary vertex question";
}

struct Answer {
    public string question;
    public string answer;
    public string type;
    public string name; // id of question it relates to

    public Answer(string q, string a, string t, string n) {
        question = q; answer = a; type = t; name = n;
    }
}

internal enum State { UNDISCOEVERED, OPENED, CLOSED };

interface IDialog {
    Question GetQuestion();
    bool SetAnswer(string answer);
    void Reverse();

    bool IsGraphValid(out string error);

    List<Answer> GetChatHistory();
}
class DialogGraph : IDialog {
    private Dictionary<string, Question> questions;
    private List<Answer> chatHistory;
    private Question currentQuestion;

    // graph algorithm private fields:

    // map from question states to ther state
    // is used by cycle detecting algorithm
    private Dictionary<string, State> DFSstates;
    private ICollection<Question> CollectionQ => questions.Values;     
    public DialogGraph(List<Question> inputQuestions) {

        // 1. foreach question in input list, add it to dict by its name 
        // 2. initialize chatHistory 
        // 3. add start and end question -> vyhrazena semantika 
        // 4. connect start and end vertex correctly to dialog graph
        // 5. set current question to start 
        // 6. run all dialog graph correctness tests
        questions = new Dictionary<string, Question>();
        foreach(Question q in inputQuestions) 
            questions[q.name] = q;

        chatHistory = new List<Answer>();
        questions["START"] = new BoundaryVertex("START", new List<Edge>() { new Edge( inputQuestions[0].name ) }) ;
        questions["END"] = new BoundaryVertex("END");
        /// !!!!! did not solve approach how to mark edges from last question to end 
        /// 

        currentQuestion = questions["START"];

        if ( GraphIsInvalid( out string error ))
            throw new Exception(error);
    }

    private bool GraphIsInvalid( out string allErrors) {

        // invalid type of graphs that can occur: 
        /*      1. Names of input questions are not unique
         *      2. Input questions names contain reserved names START or END 
         *      3. END question does not have zero out edges
         *      4. END is not reachable : no existing edge points to it
         *      5. At least one edge points to question that does not exist 
         *      6. At least one edge points to START
         *      7. At least one edge points to same question as it points from (reflexivni hrana)
         *      9. At least one edge points from START to END 
         *      10. Not all paths lead to END => some nonEND question has zero out edges
         *      11. Created graph contains cycles
         */
        bool problem = false;
        List<string> messages = new();

        if ( NotUniqueNames_ReservedNamesViolation(out string error) ) {
            problem = true; messages.Add(error);
        }

        if ( EdgesAreInvalid( out error) ) {
            problem = true; messages.Add(error);
        }

        if ( GraphContainsCycles(  out error ) ) {
            problem = true; messages.Add(error);
        }

        allErrors = "";
        if (problem)
            foreach (string s in messages)
                allErrors += $" ⚠❎>>> {s} <<<❎⚠ \n ";

        return problem;
    }

    private bool NotUniqueNames_ReservedNamesViolation(  out string error ) {
        HashSet<string> memory = new();
        error = "";
        foreach(Question q in CollectionQ ) {
            string name = q.name;
            if(memory.Contains(name)) {
                error = $"Unfortunatelly, the list of questions contains pair of questions with the same name: '{name}' 🙄🙄";
                return true;
            }
            if( name == "START" || name == "END" ) {
                error = "Sadly, some of the input questions contain reserved names: 'START' or 'END'. 😳😳";
                return true;
            }
            memory.Add(name);
        }
        return false;
    }

    private bool EdgesAreInvalid( out string error) {
        // Jiz zkontrolovane a tedy garantovano:
        //      - unikatni nazvy otazek

        // Vrat pravda pokud nektery z nasledujicich tvrzeni je pravda: 
        //      *1. End is not reachable (does not have in edge)
        //      *2. End has one or more out edges
        //      *3. At least one edge points to question that does not exist 
        //      *4. At least one edge points to START
        //      *5. At least one edge points to same question as it points from (reflexivni hrana)
        //      *6. At least one edge points from START to END
        //      *7. At least one nonEND question has 0 out edges. ( leads to nowhere )
        // Jinak vrat nepravda
        error = "";
        
        if( questions["END"].options.Count != 0 ) {
            error = "END must not have any out edge! 🚨🚨";
            return true;
        }

        bool endNOTReachable = true;
        foreach (Question q in CollectionQ) {
            string self = q.name;
            foreach (Edge e in q.options) {
                string to = e.To;
                if (!questions.ContainsKey(to)) {
                    error = $"There is an edge that is pointing to question: ${to}. But guess what. That question does not exist. 😂😂";
                    return true;
                }
                if (to == "START") {
                    error = "Some edge is pointing to 'START' 😜😜";
                    return true;
                }
                if (to == "END")
                    endNOTReachable = false;
                if(to == self) {
                    error = $"Question with the name: '{self}' has edge that is pointing to the same question. 🤦🤦";
                    return true;
                }

            }
            if (q.name != "END" && q.options.Count == 0) {
                error = $"Question ${q.name} has no out edges and thus leads to nowhere! 🤭🤭";
                return true;
            }
        }

        if (endNOTReachable) {
            error = "I did not find any edges pointing to END. 🤷🤷";
            return true;
        }

        foreach(Edge e in questions["START"].options )
            if(e.To == "END") {
                error = "Question START has edge pointing to END. I believe that will not work as a nonempty dialog. 😞😞";
                return true;
            }

        return false; // all single-edge checks passed, single edges seems to be valid! :) 
    }

    // Jiz zkontrolovane a tedy garantovano:
    //      - unikatni nazvy otazek
    //      - hrany jsou validni (list 6 ruznych checku)

    // poustej DFS dokud neprozkoumas vse, pokud zdetekujes otevrenou otazku, mas cyklus 
    // pokud prozkoumas vsechny otazky a netrefis otevrenou otazku graf je acyklicky
    //  Pseudocode: Pruvodce str. 119: ( modifikovano pro tento pripad )
    // 1. Pro vsechny vrcholy v 
    // 2.   stav(v) = nenalezeny 
    // 3. Dokud existuje nejaky nenalezeny vrchol v: 
    // 4.   je-li DFS(v)
    // 5.       vrat pravda
    // 6. vrat nepravda
    private bool GraphContainsCycles( out string error ) {
        error = "";

        DFSstates = new();
        foreach (Question q in CollectionQ)
            DFSstates[q.name] = State.UNDISCOEVERED;

        while(true) {
            string v = null;
            foreach (string q in DFSstates.Keys) {
                if (DFSstates[q] == State.UNDISCOEVERED) {
                    v = q; break;
                }
            }
            if (v == null)
                break;

            if( DFS(v) ) {
                error = "Graph contains cycle. :( :(";
                return true;
            }
        }
        return false; // graf is acyclic :) 
    }

    // Pseudocode procedura DFS(Vrchol v): 
    // 1. stav(v) = otevreny
    // 2. Pro vsechny nasledniky w vrcholu v: 
    // 3.   je-li stav(w) otevreny
    // 4.       vrat pravda   // zdetekovany cyklus -> problem 
    // 5.   je-li stav(w) nenalezeny 
    // 6.       bool b = DFS(w)
    // 7.       je-li (b)
    // 8.           vrat pravda // rekurzivni volani nekde v hloubce zdetekovalo cyklus -> problem 
    // 9. stav(v) uzavreny
    // 10.vrat nepravda // v teto casti jsme cyklus nenalezly 
    private bool DFS(string v) {
        DFSstates[v] = State.OPENED;
        foreach(Edge e in questions[v].options) {
            Question w = questions[e.To];
            if (DFSstates[w.name] == State.OPENED)
                return true;
            if (DFSstates[w.name] == State.UNDISCOEVERED ) 
                if (DFS(w.name)) // rekurzivni pokracovani DFS
                    return true;
        }
        DFSstates[v] = State.CLOSED;
        return false; // no cycle found
    }

    public Question GetQuestion() => currentQuestion;

    public bool SetAnswer(string answer) {
        // check if answer is legitime, if not, return false and do nothing 
        // otherwise
        // save answer, add it to chat history
        // update current question, depending on options and answer 
        Question currentQ = GetQuestion();
        bool validAnswer = currentQ.IsAnswerValid(answer, out string nextQuestion);

        if (validAnswer) {
            currentQuestion = questions[nextQuestion];
            // question, answer, type, nameOfQuestion
            Answer a = new( currentQ.question, answer, currentQ.GetType(), currentQ.name);
            chatHistory.Add(a);
        }
        
        return validAnswer;
    }

    public void Reverse() {
        // remove last answer from chatHistory 
        // update current question to be the last question of current chathistory 
        chatHistory.RemoveAt( chatHistory.Count-1 );
        string lastQuestion = chatHistory[chatHistory.Count - 1].name;
        currentQuestion = questions[lastQuestion];
    }

    public List<Answer> GetChatHistory() => chatHistory;

    public bool IsGraphValid(out string error) {
        bool result = GraphIsInvalid(out string e);
        error = e;
        return  (! result);
    }
}
