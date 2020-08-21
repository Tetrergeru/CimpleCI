u64 mul_v(u64 x, u64 y)
{
	u64 result;
	(result = 0);
	while(((y - 1) >= 0))
	{ 
		(result += x);
		(y -= 1);
	}
	return result;
}

u64 mul_r(u64 xx, u64 yy)
{
	if ((yy == 0))
	{
		return 0;
	}
	else
	{
		return (xx + mul_r(xx, (yy - 1)));
	}
}

u64 main()
{
	printd(mul_r(10,5));
}