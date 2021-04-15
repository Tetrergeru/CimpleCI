type Foo struct {
    a struct {
        b int;
        c int;
    };
    d int;
}

func Main() {
    var foo Foo;
    foo.a.b = 1;
    foo.a.c = 2;
    foo.d = 6;
    Test(foo, 7);
}

func Test(foo Foo, bar int) {
   Print(foo.a.b);
   Print(foo.a.c);
   Print(foo.d);
   Print(bar);
}