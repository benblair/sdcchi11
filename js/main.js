var peeps = [];
var mainTag = "#SDCChi";

var drawPeep = function(peep) {
    var left = peep.X;
    var top = peep.Y;
    var pic = peep.ProfilePic;
    var interests = peep.Words.split(',');
};

var normalizePeep = function(peep) {
    return {
        x: peep.X,
        y: peep.Y,
        pic: peep.ProfilePic,
        interests: peep.Words.split(',')
    };
};

var addPeep = function(peep) {
    peeps.push(normalizePeep(peep));
};

var updatePeep = function(update) {
    var action = update.action;
    var item = update.item;
    switch(action) {
        case 'add':
                addPeep(item);
            break;
        case 'modify':
            break;
        case 'delete':
            break;
        default:
            return;
    }
};

var simulatePeeps = function(callback) {
    var i;
    for(i = 0; i < 50; i++) {
        addPeep({
            X: Math.random(),
            Y: Math.random(),
            ProfilePic: "https://si0.twimg.com/profile_images/1453831880/profile-pic_normal.jpg",
            Words: "Foo,Bar,Social,Mobile"
        });
    }
    callback();
};

var height = window.innerHeight - 20;
var width = window.innerWidth - 20;
var getX = d3.scale.linear().domain([0,1]).range([200, width - 100]);
var getY = d3.scale.linear().domain([0,1]).range([80,height - 80]);
var getRadius = function(d) { return 15; }; // d3.scale.linear().domain([0,1]).range([5,10]);
var colorize = d3.scale.linear().domain([0,1]).range(["hsl(250, 50%, 50%)", "hsl(350, 100%, 50%)"]).interpolate(d3.interpolateHsl);
//var y2 = d3.scale.linear().domain([0,1]).range([height * 0.2 - 20, height * 0.8 + 20]);
var del = d3.scale.linear().domain([0,1]).range([0,1]);

var loadPeeps = function() {
    vis = d3.select("body")
        .append("svg:svg")
        .attr("width", width)
        .attr("height", height);
    
    simulatePeeps(function() {
        vis.selectAll("circle")
            .data(peeps)
            .enter().append("svg:circle")
            .attr("cx", function(d) { return getX(d.x); })
            .attr("cy", function(d) { return getY(d.y); })
            .attr("strike-width", "none")
            .attr("fill", function() { return colorize(Math.random()); })
            .attr("fill-opacity", 0.5)
            .attr("visibility", "hidden")
            .attr("r", function() { return getRadius(Math.random()); })
            .on("mouseover", function() {
                d3.select(this).transition()
                .attr("cx", function() { return getX(Math.random()); })
                .attr("cy", function() { return getY(Math.random()); })
                .delay(0)
                .duration(2000)
                //.ease("elastic", 0.5, 0.45);
                .ease("cubic-in-out");
            });
    
    d3.selectAll("circle")
        .transition()
        .attr("cx", function() { return getX(Math.random()); })
        .attr("cy", function() { return getY(Math.random()); })
        .attr("visibility", "visible")
        .delay(function(d, i) { return i * del(Math.random()); })
        .duration(1000)
        .ease("elastic", 10, 0.45);
 });
};