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
    for(i = 0; i < 500; i++) {
        addPeep({
            X: Math.random(),
            Y: Math.random(),
            ProfilePic: "https://si0.twimg.com/profile_images/1453831880/profile-pic_normal.jpg",
            Words: "Foo,Bar,Social,Mobile"
        });
    }
    callback();
};

var getX = d3.scale.linear().domain([0,1]).range([screen.width / 2 - 400,screen.width / 2 + 400]);
var getY = d3.scale.linear().domain([0,1]).range([0,600]);
var getRadius = d3.scale.linear().domain([0,1]).range([5,10]);
var colorize = d3.scale.linear().domain([0,1]).range(["hsl(250, 50%, 50%)", "hsl(350, 100%, 50%)"]).interpolate(d3.interpolateHsl);

var loadPeeps = function() {
    vis = d3.select("body")
        .append("svg:svg")
        .attr("width", screen.width)
        .attr("height", screen.height);
    simulatePeeps(function() {
        vis.selectAll("circle")
            .data(peeps)
            .enter().append("svg:circle")
            .attr("cx", function(d) { return getX(d.x); })
            .attr("cy", function(d) { return getY(d.y); })
            .attr("strike-width", "none")
            .attr("fill", function() { return colorize(Math.random()); })
            .attr("fill-opacity", 0.5)
            //.attr("visibility", "hidden")
            .attr("r", function() { return getRadius(Math.random()); });
    });  
};