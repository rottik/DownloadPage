#!/usr/bin/perl -w
$program="perl classifier.pl";
opendir (DIR, "files") or die $!;
while (my $file = readdir(DIR))
{ 
	if ($file =~ m/.*?\.txt/)
	{
	$cmd="$program $file";
	#print "\n",$file;
	system($cmd);
	print "\n";
	}
}
closedir(DIR);