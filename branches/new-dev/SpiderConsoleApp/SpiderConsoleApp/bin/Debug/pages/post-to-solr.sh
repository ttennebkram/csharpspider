#!/bin/sh

c=0;
for i in `ls | grep .html`; do
	echo "posting doc$c to Solr..."
	curl http://localhost:8983/solr/update/extract\?literal.id\=doc$c\&uprefix=attr_\&fmap.content=text\&commit=true -F myfile=@$i;
	c=$(($c + 1));
done