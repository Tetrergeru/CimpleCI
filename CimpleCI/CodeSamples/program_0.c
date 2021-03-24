u64 print_number(u64 x)
{
	u64 string;
	(string = 0x000a7825);
	return printf((&string), x);
}

u64 print_sth()
{
	print_number(0x42);
}