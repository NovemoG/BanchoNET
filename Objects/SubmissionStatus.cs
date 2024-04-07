namespace BanchoNET.Objects;

//TODO change it to flags and apply Best and BestWithModCombination accordingly
public enum SubmissionStatus : byte
{
	Failed,
	Submitted,
	Best,
	BestWithModCombination
}